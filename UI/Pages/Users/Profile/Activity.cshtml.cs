using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using
using System.Net.Http; // ✅ Añadir para HttpRequestException

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels; // Donde están tus ActivityFeedItemViewModel, etc.

public class ActivityModel : ProfileModelBase
{
    // --- Servicios inyectados ---
    private readonly IReviewService _reviewService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameListService _gameListService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ActivityModel> _logger; 


    public List<ActivityFeedItemViewModel> ActivityFeed { get; set; } = new();

    public ActivityModel(
        IReviewService reviewService,
        IGameTrackingService gameTrackingService,
        IGameListService gameListService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IModerationReportService moderationReportService,
        ILogger<ActivityModel> logger)
    {
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _gameTrackingService = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _gameListService = gameListService ?? throw new ArgumentNullException(nameof(gameListService), "IGameListService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Activity";

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("OnGetAsync: userId es nulo o vacío."); 
            TempData["StatusMessage"] = "Error: ID de usuario no proporcionado.";
            return BadRequest();
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga de actividad para el usuario '{ProfileUserId}'.", userId); 

            var userExists = await LoadProfileHeaderData(
                userId,
                _authService,
                _userManagerService,
                _friendshipService,
                _reviewService,
                _gameListService,
                _gameTrackingService);

            if (!userExists)
            {
                _logger.LogWarning("OnGetAsync: Usuario con ID '{ProfileUserId}' no encontrado al cargar la actividad.", userId); 
                TempData["StatusMessage"] = "Error: El perfil de usuario no fue encontrado.";
                return NotFound();
            }

            var reviewsTask = _reviewService.GetFriendsReviewsAsync(new List<string> { userId });
            var gameTrackingsTask = _gameTrackingService.GetTrackingsByUserAsync(userId);
            var gameListsTask = _gameListService.GetUserListsAsync(userId);

            await Task.WhenAll(reviewsTask, gameTrackingsTask, gameListsTask);

            var reviews = reviewsTask.Result ?? new List<ReviewDTO>();
            var gameTrackings = gameTrackingsTask.Result ?? new List<GameTrackingDto>();
            var gameLists = gameListsTask.Result ?? new List<GameListDTO>();

            foreach (var r in reviews)
            {
                if (r == null || r.GameId == Guid.Empty || string.IsNullOrWhiteSpace(r.Id))
                {
                    _logger.LogWarning("OnGetAsync: Reseña inválida (nula, GameId vacío o Id nulo) encontrada para el usuario '{ProfileUserId}'. Saltando.", userId); 
                    continue;
                }
                try
                {
                    var gamePreview = await _gameService.GetGamePreviewByIdAsync(r.GameId);
                    if (gamePreview != null)
                    {
                        ActivityFeed.Add(new ReviewActivityViewModel
                        {
                            Type = "Review",
                            Timestamp = r.CreatedAt,
                            UserId = r.UserId,
                            Username = ProfileHeader.Username,
                            UserAvatarUrl = ProfileHeader.AvatarUrl,
                            ReviewId = r.Id,
                            GameId = r.GameId.ToString(),
                            GameTitle = gamePreview.Title,
                            GameImageUrl = gamePreview.HeaderUrl,
                            Content = r.Content,
                            Rating = r.Rating,
                            LikesCount = r.Likes
                        });
                    }
                    else
                    {
                        _logger.LogWarning("OnGetAsync: GamePreview nulo para el juego '{GameId}' en la reseña '{ReviewId}'.", r.GameId, r.Id); 
                    }
                }
                catch (HttpRequestException ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: HttpRequestException al obtener GamePreview para el juego '{GameId}' de la reseña '{ReviewId}'.", r.GameId, r.Id);
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: Error inesperado al mapear reseña '{ReviewId}' para el usuario '{ProfileUserId}'.", r.Id, userId);
                }
            }
            _logger.LogInformation("OnGetAsync: {Count} reseñas procesadas para el feed.", reviews.Count);

            foreach (var gt in gameTrackings)
            {
                if (gt == null || string.IsNullOrWhiteSpace(gt.GameId) || string.IsNullOrWhiteSpace(gt.Id.ToString()))
                {
                    _logger.LogWarning("OnGetAsync: Seguimiento de juego inválido (nulo, GameId o Id nulo) encontrado para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    if (!Guid.TryParse(gt.GameId, out Guid parsedGameId))
                    {
                        _logger.LogWarning("OnGetAsync: GameId '{GameId}' en el seguimiento '{TrackingId}' no es un GUID válido para el usuario '{ProfileUserId}'.", gt.GameId, gt.Id, userId);
                        continue;
                    }

                    var gamePreview = await _gameService.GetGamePreviewByIdAsync(parsedGameId);
                    if (gamePreview != null)
                    {
                        ActivityFeed.Add(new GameLogActivityViewModel
                        {
                            Type = "GameLog",
                            Timestamp = gt.LastUpdatedAt, 
                            UserId = gt.UserId,
                            Username = ProfileHeader.Username,
                            UserAvatarUrl = ProfileHeader.AvatarUrl,
                            GameTrackingId = gt.Id,
                            GameId = gt.GameId,
                            GameTitle = gamePreview.Title,
                            GameImageUrl = gamePreview.HeaderUrl,
                            Status = gt.Status
                        });
                    }
                    else
                    {
                        _logger.LogWarning("OnGetAsync: GamePreview nulo para el juego '{GameId}' en el seguimiento '{TrackingId}'.", parsedGameId, gt.Id);
                    }
                }
                catch (HttpRequestException ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: HttpRequestException al obtener GamePreview para el juego '{GameId}' del seguimiento '{TrackingId}'.", gt.GameId, gt.Id);
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: Error inesperado al mapear seguimiento '{TrackingId}' para el usuario '{ProfileUserId}'.", gt.Id, userId);
                }
            }
            _logger.LogInformation("OnGetAsync: {Count} seguimientos de juegos procesados para el feed.", gameTrackings.Count);


            foreach (var gl in gameLists)
            {
                if (gl == null || string.IsNullOrWhiteSpace(gl.Id))
                {
                    _logger.LogWarning("OnGetAsync: Lista de juegos inválida (nula o Id nulo) encontrada para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    ActivityFeed.Add(new GameListActivityViewModel
                    {
                        Type = "GameList",
                        Timestamp = gl.CreatedAt,
                        UserId = gl.UserId,
                        Username = ProfileHeader.Username,
                        UserAvatarUrl = ProfileHeader.AvatarUrl,
                        ListId = gl.Id,
                        ListName = gl.Name,
                        Description = gl.Description
                    });
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: Error inesperado al mapear lista '{ListId}' para el usuario '{ProfileUserId}'.", gl.Id, userId);
                }
            }
            _logger.LogInformation("OnGetAsync: {Count} listas de juegos procesadas para el feed.", gameLists.Count);


            ActivityFeed = ActivityFeed.OrderByDescending(item => item.Timestamp).ToList();
            _logger.LogInformation("OnGetAsync: Feed de actividad ordenado y listo. Total de items: {ItemCount}", ActivityFeed.Count);


            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar actividad del usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar actividad del usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar actividad del usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Problema de conexión al cargar la actividad. Por favor, verifica tu internet.";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar la actividad del usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Ocurrió un error inesperado al cargar la actividad. Por favor, inténtalo de nuevo más tarde.";
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
            var success = await _friendshipService.SendRequestAsync(loggedInUserId, profileUserId);
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
            var success = await _friendshipService.AcceptRequestAsync(requestId);
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
            var success = await _friendshipService.RejectRequestAsync(requestId);
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

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostReportUserAsync(string profileUserId, string reason)
    {
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