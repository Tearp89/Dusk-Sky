using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using
using System.Net.Http; // ✅ Para HttpRequestException



public class ListsProfileModel : ProfileModelBase
{
    // --- Servicios necesarios ---
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService_private; 
    private readonly IUserManagerService _userManagerService_private; 
    private readonly IFriendshipService _friendshipService_private; 
    private readonly IReviewService _reviewService_private;
    private readonly IGameTrackingService _gameTrackingService_private;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ListsProfileModel> _logger; 

    public List<GameListPreviewViewModel> UserLists { get; set; } = new();

    public ListsProfileModel(
        IGameListService gameListService,
        IGameListItemService gameListItemService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameTrackingService gameTrackingService,
        IModerationReportService moderationReportService,
        ILogger<ListsProfileModel> logger) 
    {
        _gameListService = gameListService ?? throw new ArgumentNullException(nameof(gameListService), "IGameListService no puede ser nulo.");
        _gameListItemService = gameListItemService ?? throw new ArgumentNullException(nameof(gameListItemService), "IGameListItemService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _authService_private = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _userManagerService_private = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _friendshipService_private = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService no puede ser nulo.");
        _reviewService_private = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _gameTrackingService_private = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Lists";

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("OnGetAsync: userId es nulo o vacío. Redirigiendo a BadRequest.");
            TempData["StatusMessage"] = "Error: ID de usuario no proporcionado.";
            return BadRequest();
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga de listas para el usuario '{ProfileUserId}'.", userId);

            var userExists = await LoadProfileHeaderData(
                userId,
                _authService_private,
                _userManagerService_private,
                _friendshipService_private,
                _reviewService_private,
                _gameListService, 
                _gameTrackingService_private);

            if (!userExists)
            {
                _logger.LogWarning("OnGetAsync: Usuario con ID '{ProfileUserId}' no encontrado al cargar listas.", userId);
                TempData["StatusMessage"] = "Error: El perfil de usuario no fue encontrado.";
                return NotFound();
            }

            var allLists = await _gameListService.GetUserListsAsync(userId);
            allLists ??= new List<GameListDTO>(); 

            var filteredLists = new List<GameListDTO>();

            string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (loggedInUserId == userId)
            {
                _logger.LogDebug("OnGetAsync: Cargando todas las listas (públicas y privadas) para el propio usuario '{ProfileUserId}'.", userId);
                filteredLists = allLists.OrderByDescending(l => l.CreatedAt).ToList();
            }
            else
            {
                _logger.LogDebug("OnGetAsync: Cargando solo listas públicas para el usuario '{ProfileUserId}' (visitante).", userId);
                filteredLists = allLists
                    .Where(l => l.IsPublic)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
            }
            _logger.LogInformation("OnGetAsync: {Count} listas filtradas para mostrar al usuario '{ProfileUserId}'.", filteredLists.Count, userId);


            var listTasks = new List<Task<GameListPreviewViewModel>>();
            foreach (var listDto in filteredLists)
            {
                if (listDto == null || string.IsNullOrWhiteSpace(listDto.Id))
                {
                    _logger.LogWarning("OnGetAsync: GameListDTO nulo o con ID vacío encontrado al procesar listas para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                listTasks.Add(Task.Run(async () =>
                {
                    var previewViewModel = new GameListPreviewViewModel
                    {
                        Id = listDto.Id,
                        UserId = listDto.UserId,
                        Name = listDto.Name,
                        Description = listDto.Description,
                        IsPublic = listDto.IsPublic,
                        CreatedAt = listDto.CreatedAt,
                        LikedBy = listDto.LikedBy 
                    };

                    List<GameListItemDTO> listItems = new List<GameListItemDTO>();
                    try
                    {
                        listItems = await _gameListItemService.GetItemsByListIdAsync(listDto.Id) ?? new List<GameListItemDTO>();
                        _logger.LogDebug("OnGetAsync: {ItemCount} items obtenidos para la lista '{ListId}'.", listItems.Count, listDto.Id);
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: HttpRequestException al obtener ítems para la lista '{ListId}'.", listDto.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: Error inesperado al obtener ítems para la lista '{ListId}'.", listDto.Id);
                    }
                    
                    var gameImageTasks = new List<Task<GamePreviewDTO?>>();
                    foreach (var item in listItems.Take(4))
                    {
                        if (item == null || item.GameId == Guid.Empty)
                        {
                            _logger.LogWarning("OnGetAsync: Item de lista inválido (nulo o GameId vacío) en la lista '{ListId}'. Saltando.", listDto.Id);
                            continue;
                        }
                        try
                        {
                            gameImageTasks.Add(_gameService.GetGamePreviewByIdAsync(item.GameId));
                        }
                        catch (HttpRequestException ex)
                        {
                            _logger.LogError(ex, "OnGetAsync: HttpRequestException al obtener GamePreview para el juego '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            gameImageTasks.Add(Task.FromResult<GamePreviewDTO?>(null)); 
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "OnGetAsync: Error inesperado al obtener GamePreview para el juego '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            gameImageTasks.Add(Task.FromResult<GamePreviewDTO?>(null));
                        }
                    }
                    var gamePreviews = await Task.WhenAll(gameImageTasks);
                    previewViewModel.GameImageUrls = gamePreviews
                                                             .Where(gp => gp != null && !string.IsNullOrEmpty(gp.HeaderUrl))
                                                             .Select(gp => gp!.HeaderUrl)
                                                             .ToList();
                    _logger.LogDebug("OnGetAsync: {ImageCount} imágenes de juegos obtenidas para la lista '{ListId}'.", previewViewModel.GameImageUrls.Count, listDto.Id);

                    return previewViewModel;
                }));
            }
            UserLists = (await Task.WhenAll(listTasks)).Where(vm => vm != null).ToList()!; 
            _logger.LogInformation("OnGetAsync: Carga de todas las GameListPreviewViewModel completada. Total: {Count}.", UserLists.Count);


            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar listas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar listas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar listas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Problema de conexión al cargar las listas. Por favor, verifica tu internet.";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar las listas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Ocurrió un error inesperado al cargar las listas. Por favor, inténtalo de nuevo más tarde.";
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
            var success = await _friendshipService_private.AcceptRequestAsync(requestId); 
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
            var success = await _friendshipService_private.RejectRequestAsync(requestId); 
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