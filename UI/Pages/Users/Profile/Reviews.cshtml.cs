using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using
using System.Net.Http; // ✅ Para HttpRequestException

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;

public class ReviewsProfileModel : ProfileModelBase
{
    // --- Servicios necesarios para esta página y para la clase base ---
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService_private; // Usar nombre diferente si _authService es de la base
    private readonly IUserManagerService _userManagerService_private; // Usar nombre diferente
    private readonly IFriendshipService _friendshipService_private; // Usar nombre diferente
    private readonly IGameListService _listService_private; // Usar nombre diferente
    private readonly IGameService _gameService;
    private readonly IGameTrackingService _gameTrackingService_private; // Usar nombre diferente
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ReviewsProfileModel> _logger; // ✅ Declaración del logger


    // --- Propiedad para guardar las reseñas de este usuario ---
    public List<ReviewCardViewModel> UserReviews { get; set; } = new();

    public ReviewsProfileModel(
        IReviewService reviewService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IGameListService listService,
        IGameService gameService,
        IGameTrackingService gameTrackingService,
        IModerationReportService moderationReportService,
        ILogger<ReviewsProfileModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para todos los servicios y el logger
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _authService_private = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _userManagerService_private = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _friendshipService_private = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService no puede ser nulo.");
        _listService_private = listService ?? throw new ArgumentNullException(nameof(listService), "IGameListService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _gameTrackingService_private = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Reviews";

        // ✅ Validar que userId no sea nulo o vacío
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("OnGetAsync: userId es nulo o vacío. Redirigiendo a BadRequest.");
            TempData["StatusMessage"] = "Error: ID de usuario no proporcionado.";
            return BadRequest();
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga de reseñas para el usuario '{ProfileUserId}'.", userId);

            // Llamada a LoadProfileHeaderData que ahora usa los campos protegidos de ProfileModelBase
            var userExists = await LoadProfileHeaderData(
                userId,
                _authService_private, // Pasa tus servicios privados al método base si es necesario
                _userManagerService_private,
                _friendshipService_private,
                _reviewService, // Aquí va el reviewService de esta clase (ya se validó su nullabilidad)
                _listService_private,
                _gameTrackingService_private);

            if (!userExists)
            {
                _logger.LogWarning("OnGetAsync: Usuario con ID '{ProfileUserId}' no encontrado al cargar reseñas.", userId);
                TempData["StatusMessage"] = "Error: El perfil de usuario no fue encontrado.";
                return NotFound();
            }

            var reviewDocs = await _reviewService.GetFriendsReviewsAsync(new List<string> { userId });
            reviewDocs ??= new List<ReviewDTO>(); // ✅ Asegurar que reviewDocs no sea nulo

            if (reviewDocs.Any())
            {
                var reviewTasks = reviewDocs.Select(dto => MapToViewModelAsync(dto, ProfileHeader.Username, ProfileHeader.AvatarUrl)); // Pasa ProfileHeader datos
                UserReviews = (await Task.WhenAll(reviewTasks)).Where(vm => vm != null).ToList()!; // ✅ Filtrar ViewModels nulos
                _logger.LogInformation("OnGetAsync: {Count} reseñas procesadas y cargadas para el usuario '{ProfileUserId}'.", UserReviews.Count, userId);
            }
            else
            {
                _logger.LogInformation("OnGetAsync: No se encontraron reseñas para el usuario '{ProfileUserId}'.", userId);
            }

            return Page();
        }
        catch (ArgumentException ex) // ✅ Catch específico para ArgumentException
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar reseñas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar reseñas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red generales
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar reseñas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Problema de conexión al cargar las reseñas. Por favor, verifica tu internet.";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar las reseñas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Ocurrió un error inesperado al cargar las reseñas. Por favor, inténtalo de nuevo más tarde.";
            return RedirectToPage("/Error");
        }
    }

    // MODIFICADO: MapToViewModelAsync con try-catch interno y parámetros para datos del perfil
    private async Task<ReviewCardViewModel> MapToViewModelAsync(ReviewDTO reviewDto, string profileUsername, string profileAvatarUrl)
    {
        // ✅ Validar reviewDto no sea nulo
        if (reviewDto == null)
        {
            _logger.LogWarning("MapToViewModelAsync: Se intentó mapear un ReviewDTO nulo.");
            return new ReviewCardViewModel { /* valores por defecto */ };
        }
        // ✅ Validar reviewDto.GameId
        if (reviewDto.GameId == Guid.Empty)
        {
            _logger.LogWarning("MapToViewModelAsync: ReviewDTO '{ReviewId}' tiene un GameId vacío.", reviewDto.Id);
            // Podrías devolver un ViewModel con valores de error o simplemente GameTitle/ImageUrl por defecto
        }

        try
        {
            var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);

            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(), // Asegura que ID no sea nulo
                GameId = reviewDto.GameId.ToString(),
                GameTitle = game?.Title ?? "Unknown Game",
                GameImageUrl = game?.HeaderUrl ?? "/images/noImage.png",
                UserId = reviewDto.UserId,
                UserName = profileUsername, // Usar el username del ProfileHeader
                UserAvatarUrl = profileAvatarUrl, // Usar el avatar del ProfileHeader
                Content = reviewDto.Content,
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con IGameService
        {
            _logger.LogError(ex, "MapToViewModelAsync: HttpRequestException al obtener GamePreview para el juego '{GameId}' de la reseña '{ReviewId}'. Mensaje: {Message}", reviewDto.GameId, reviewDto.Id, ex.Message);
            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(),
                GameId = reviewDto.GameId.ToString(),
                GameTitle = "Error de Carga de Juego",
                GameImageUrl = "/images/error.png",
                UserId = reviewDto.UserId,
                UserName = profileUsername,
                UserAvatarUrl = profileAvatarUrl,
                Content = reviewDto.Content,
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción durante el mapeo
        {
            _logger.LogError(ex, "MapToViewModelAsync: Error inesperado al mapear ReviewDTO '{ReviewId}'. Mensaje: {Message}", reviewDto.Id, ex.Message);
            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(),
                GameId = reviewDto.GameId.ToString(),
                GameTitle = "Error General de Carga",
                GameImageUrl = "/images/error.png",
                UserId = reviewDto.UserId,
                UserName = profileUsername,
                UserAvatarUrl = profileAvatarUrl,
                Content = reviewDto.Content,
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
    }
    
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostSendRequestAsync(string profileUserId)
    {
        // ✅ Validar profileUserId
        if (string.IsNullOrWhiteSpace(profileUserId))
        {
            _logger.LogWarning("OnPostSendRequestAsync: profileUserId es nulo o vacío.");
            TempData["StatusMessage"] = "Error: ID de perfil no válido.";
            return BadRequest();
        }

        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        // ✅ Validar loggedInUserId
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            _logger.LogWarning("OnPostSendRequestAsync: Usuario no autenticado intentando enviar solicitud a '{ProfileUserId}'.", profileUserId);
            return Forbid();
        }

        if (loggedInUserId == profileUserId)
        {
            _logger.LogWarning("OnPostSendRequestAsync: Usuario '{UserId}' intentó enviarse una solicitud de amistad a sí mismo.", loggedInUserId);
            TempData["StatusMessage"] = "Error: No puedes enviarte una solicitud de amistad a ti mismo.";
            return RedirectToPage(new { userId = profileUserId });
        }

        try
        {
            // Usar _friendshipService_private si se asigna a uno privado en el constructor de esta clase
            var success = await _friendshipService_private.SendRequestAsync(loggedInUserId, profileUserId); 
            if (success)
            {
                _logger.LogInformation("OnPostSendRequestAsync: Solicitud de amistad enviada de '{SenderId}' a '{ReceiverId}'.", loggedInUserId, profileUserId);
                TempData["StatusMessage"] = "Solicitud de amistad enviada.";
            }
            else
            {
                _logger.LogWarning("OnPostSendRequestAsync: Falló el envío de solicitud de amistad de '{SenderId}' a '{ReceiverId}'. Ya existe una pendiente o ya son amigos.", loggedInUserId, profileUserId);
                TempData["StatusMessage"] = "Error: No se pudo enviar la solicitud de amistad. Ya existe una pendiente o ya son amigos.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostSendRequestAsync: HttpRequestException al enviar solicitud de amistad de '{SenderId}' a '{ReceiverId}'. Mensaje: {Message}", loggedInUserId, profileUserId, ex.Message);
            TempData["StatusMessage"] = "Error de conexión al enviar la solicitud de amistad.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostSendRequestAsync: Error inesperado al enviar solicitud de amistad de '{SenderId}' a '{ReceiverId}'. Mensaje: {Message}", loggedInUserId, profileUserId, ex.Message);
            TempData["StatusMessage"] = "Error: Ocurrió un error inesperado al enviar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAcceptRequestAsync(string requestId, string profileUserId)
    {
        // ✅ Validar parámetros
        if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(profileUserId))
        {
            _logger.LogWarning("OnPostAcceptRequestAsync: requestId o profileUserId es nulo/vacío.");
            TempData["StatusMessage"] = "Error: Datos de solicitud incompletos.";
            return BadRequest();
        }

        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            _logger.LogWarning("OnPostAcceptRequestAsync: Usuario no autenticado intentando aceptar solicitud '{RequestId}'.", requestId);
            return Forbid();
        }

        try
        {
            var success = await _friendshipService_private.AcceptRequestAsync(requestId); // Usar _friendshipService_private
            if (success)
            {
                _logger.LogInformation("OnPostAcceptRequestAsync: Solicitud de amistad '{RequestId}' aceptada por el usuario '{AccepterId}'.", requestId, loggedInUserId);
                TempData["StatusMessage"] = "Solicitud de amistad aceptada. ¡Ahora son amigos!";
            }
            else
            {
                _logger.LogWarning("OnPostAcceptRequestAsync: Falló al aceptar la solicitud de amistad '{RequestId}' por el usuario '{AccepterId}'.", requestId, loggedInUserId);
                TempData["StatusMessage"] = "Error: No se pudo aceptar la solicitud de amistad.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostAcceptRequestAsync: HttpRequestException al aceptar solicitud de amistad '{RequestId}' por el usuario '{AccepterId}'. Mensaje: {Message}", requestId, loggedInUserId, ex.Message);
            TempData["StatusMessage"] = "Error de conexión al aceptar la solicitud de amistad.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostAcceptRequestAsync: Error inesperado al aceptar solicitud de amistad '{RequestId}' por el usuario '{AccepterId}'. Mensaje: {Message}", requestId, loggedInUserId, ex.Message);
            TempData["StatusMessage"] = "Error: Ocurrió un error inesperado al aceptar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostRejectRequestAsync(string requestId, string profileUserId)
    {
        // ✅ Validar parámetros
        if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(profileUserId))
        {
            _logger.LogWarning("OnPostRejectRequestAsync: requestId o profileUserId es nulo/vacío.");
            TempData["StatusMessage"] = "Error: Datos de solicitud incompletos.";
            return BadRequest();
        }

        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            _logger.LogWarning("OnPostRejectRequestAsync: Usuario no autenticado intentando rechazar solicitud '{RequestId}'.", requestId);
            return Forbid();
        }

        try
        {
            var success = await _friendshipService_private.RejectRequestAsync(requestId); // Usar _friendshipService_private
            if (success)
            {
                _logger.LogInformation("OnPostRejectRequestAsync: Solicitud de amistad '{RequestId}' rechazada por el usuario '{RejecterId}'.", requestId, loggedInUserId);
                TempData["StatusMessage"] = "Solicitud de amistad rechazada.";
            }
            else
            {
                _logger.LogWarning("OnPostRejectRequestAsync: Falló al rechazar la solicitud de amistad '{RequestId}' por el usuario '{RejecterId}'.", requestId, loggedInUserId);
                TempData["StatusMessage"] = "Error: No se pudo rechazar la solicitud de amistad.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostRejectRequestAsync: HttpRequestException al rechazar solicitud de amistad '{RequestId}' por el usuario '{RejecterId}'. Mensaje: {Message}", requestId, loggedInUserId, ex.Message);
            TempData["StatusMessage"] = "Error de conexión al rechazar la solicitud de amistad.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostRejectRequestAsync: Error inesperado al rechazar solicitud de amistad '{RequestId}' por el usuario '{RejecterId}'. Mensaje: {Message}", requestId, loggedInUserId, ex.Message);
            TempData["StatusMessage"] = "Error: Ocurrió un error inesperado al rechazar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }
    
    [ValidateAntiForgeryToken] // Siempre para formularios POST que modifican datos
    public async Task<IActionResult> OnPostReportUserAsync(string profileUserId, string reason)
    {
        // ✅ Validar parámetros de entrada
        if (string.IsNullOrWhiteSpace(profileUserId))
        {
            _logger.LogWarning("OnPostReportUserAsync: profileUserId es nulo o vacío.");
            TempData["StatusMessage"] = "Error: ID de perfil a reportar no proporcionado.";
            return BadRequest();
        }
        if (string.IsNullOrWhiteSpace(reason))
        {
            _logger.LogWarning("OnPostReportUserAsync: Razón de reporte vacía para el usuario '{ProfileUserId}'.", profileUserId);
            TempData["StatusMessage"] = "Error: El motivo del reporte no puede estar vacío.";
            return RedirectToPage(new { userId = profileUserId });
        }

        try
        {
            string? reporterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reporterUserId))
            {
                _logger.LogWarning("OnPostReportUserAsync: Usuario no autenticado intentando reportar a '{ProfileUserId}'.", profileUserId);
                TempData["StatusMessage"] = "Error: Debes iniciar sesión para reportar un usuario.";
                return Unauthorized();
            }

            if (reporterUserId == profileUserId)
            {
                _logger.LogWarning("OnPostReportUserAsync: Usuario '{ReporterId}' intentó reportarse a sí mismo.", reporterUserId);
                TempData["StatusMessage"] = "Error: No puedes reportarte a ti mismo.";
                return RedirectToPage(new { userId = profileUserId });
            }

            var report = new ReportDTO
            {
                Id = Guid.NewGuid().ToString(),
                ReportedUserId = profileUserId,
                Reason = reason,
                ContentType = "User",
                Status = "pending",
                ReportedAt = DateTime.UtcNow
            };

            bool success = await _moderationService.CreateAsync(report);

            if (success)
            {
                _logger.LogInformation("OnPostReportUserAsync: Usuario '{ProfileUserId}' reportado exitosamente por '{ReporterId}'. Razón: '{Reason}'", profileUserId, reporterUserId, reason);
                TempData["SuccessMessage"] = "El usuario ha sido reportado con éxito. Gracias por tu ayuda, investigaremos tu reporte.";
            }
            else
            {
                _logger.LogError("OnPostReportUserAsync: Falló el envío del reporte para el usuario '{ProfileUserId}' por '{ReporterId}'.", profileUserId, reporterUserId);
                TempData["StatusMessage"] = "Error: No se pudo enviar el reporte. Por favor, inténtalo de nuevo más tarde.";
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostReportUserAsync: HttpRequestException al reportar usuario '{ProfileUserId}' por '{ReporterId}'. Mensaje: {Message}", profileUserId, User.FindFirstValue(ClaimTypes.NameIdentifier), ex.Message);
            TempData["StatusMessage"] = "Error de conexión al enviar el reporte.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostReportUserAsync: Error inesperado al reportar usuario '{ProfileUserId}' por '{ReporterId}'. Mensaje: {Message}", profileUserId, User.FindFirstValue(ClaimTypes.NameIdentifier), ex.Message);
            TempData["StatusMessage"] = "Error: Ocurrió un error inesperado al enviar el reporte.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }
}