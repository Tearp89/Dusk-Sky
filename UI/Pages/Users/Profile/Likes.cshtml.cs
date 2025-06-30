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

public class LikesModel : ProfileModelBase
{
    // --- Servicios inyectados ---
    // NOTA: Los servicios utilizados solo en LoadProfileHeaderData se pueden pasar directamente allí,
    // pero si se usan en otros métodos de LikesModel, deben ser campos privados aquí.
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService_private; // Usar nombre diferente si _authService es de la base
    private readonly IUserManagerService _userManagerService_private; // Usar nombre diferente
    private readonly IFriendshipService _friendshipService_private; // Usar nombre diferente
    private readonly IReviewService _reviewService_private;
    private readonly IGameListService _gameListService_private;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<LikesModel> _logger; // ✅ Declaración del logger

    // --- Propiedad para la lista de juegos "likeados" ---
    public List<GamePreviewDTO> LikedGames { get; set; } = new();

    public LikesModel(
        IGameTrackingService gameTrackingService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService gameListService,
        IModerationReportService moderationReportService,
        ILogger<LikesModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para todos los servicios y el logger
        _gameTrackingService = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        // Asignación a campos privados (no los protegidos de la base) si son usados directamente aquí.
        // Si ProfileModelBase los expone como protegidos y los usas desde allí, no necesitas estos _private.
        _authService_private = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _userManagerService_private = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _friendshipService_private = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService no puede ser nulo.");
        _reviewService_private = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _gameListService_private = gameListService ?? throw new ArgumentNullException(nameof(gameListService), "IGameListService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Likes"; // Para que la pestaña "Likes" se resalte

        // ✅ Validar que userId no sea nulo o vacío
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("OnGetAsync: userId es nulo o vacío. Redirigiendo a BadRequest.");
            TempData["StatusMessage"] = "Error: ID de usuario no proporcionado.";
            return BadRequest();
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga de juegos likeados para el usuario '{ProfileUserId}'.", userId);

            // 1. Cargar los datos del encabezado del perfil
            var userExists = await LoadProfileHeaderData(
                userId,
                _authService_private, // Pasa tus servicios privados al método base si es necesario
                _userManagerService_private,
                _friendshipService_private,
                _reviewService_private,
                _gameListService_private,
                _gameTrackingService);

            if (!userExists)
            {
                _logger.LogWarning("OnGetAsync: Usuario con ID '{ProfileUserId}' no encontrado al cargar juegos likeados.", userId);
                TempData["StatusMessage"] = "Error: El perfil de usuario no fue encontrado.";
                return NotFound();
            }

            // 2. Obtener los IDs de los juegos que el usuario ha "likeado"
            var likedGameIdsStrings = await _gameTrackingService.GetLikedGameIdsAsync(userId);
            likedGameIdsStrings ??= new List<string>(); // ✅ Asegurar que no sea nulo

            // Convertir los IDs de string a Guid y filtrarlos si son inválidos
            var likedGameIds = likedGameIdsStrings
                .Where(id =>
                {
                    if (Guid.TryParse(id, out _)) return true;
                    _logger.LogWarning("OnGetAsync: ID de juego liked inválido '{InvalidId}' encontrado para el usuario '{ProfileUserId}'.", id, userId); // ✅ Registro de advertencia
                    return false;
                })
                .Select(Guid.Parse)
                .ToList();
            
            _logger.LogInformation("OnGetAsync: {Count} IDs de juegos gustados encontrados para el usuario '{ProfileUserId}'.", likedGameIds.Count, userId);


            // 3. Obtener los detalles de los juegos "likeados" de forma concurrente
            var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
            foreach (var gameId in likedGameIds)
            {
                if (gameId == Guid.Empty) // Este check es redundante después del Where(Guid.TryParse) pero puede ser una buena defensa si el origen cambia.
                {
                    _logger.LogWarning("OnGetAsync: GameId vacío en la lista de likedGameIds para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    gamePreviewTasks.Add(_gameService.GetGamePreviewByIdAsync(gameId));
                }
                catch (HttpRequestException ex) // ✅ Catch específico para errores de red
                {
                    _logger.LogError(ex, "OnGetAsync: HttpRequestException al obtener GamePreview para el juego '{GameId}' gustado por el usuario '{ProfileUserId}'.", gameId, userId);
                    gamePreviewTasks.Add(Task.FromResult<GamePreviewDTO?>(null)); // Añadir una tarea nula para no romper Task.WhenAll
                }
                catch (Exception ex) // ✅ Catch general
                {
                    _logger.LogError(ex, "OnGetAsync: Error inesperado al obtener GamePreview para el juego '{GameId}' gustado por el usuario '{ProfileUserId}'.", gameId, userId);
                    gamePreviewTasks.Add(Task.FromResult<GamePreviewDTO?>(null));
                }
            }

            var gamePreviews = await Task.WhenAll(gamePreviewTasks);

            // 4. Filtrar los juegos que realmente se encontraron y asignarlos a la propiedad
            LikedGames = gamePreviews.Where(gp => gp != null).ToList()!;
            _logger.LogInformation("OnGetAsync: {Count} juegos gustados cargados exitosamente para el usuario '{ProfileUserId}'.", LikedGames.Count, userId);

            return Page();
        }
        catch (ArgumentException ex) // ✅ Catch específico para ArgumentException
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar juegos likeados para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar juegos likeados para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red generales
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar juegos likeados para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Problema de conexión al cargar los juegos gustados. Por favor, verifica tu internet.";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar los juegos likeados para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Ocurrió un error inesperado al cargar los juegos gustados. Por favor, inténtalo de nuevo más tarde.";
            return RedirectToPage("/Error");
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
            var success = await _friendshipService_private.SendRequestAsync(loggedInUserId, profileUserId); // Usar _friendshipService_private
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