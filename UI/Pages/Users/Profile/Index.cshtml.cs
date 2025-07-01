using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; 
using System.Net.Http; 



public class ProfileModel : ProfileModelBase
{
    private readonly IUserManagerService _userManagerService_private;
    private readonly IReviewService _reviewService_private;
    private readonly IGameListService _listService_private;
    private readonly IGameTrackingService _gameTrackingService_private;
    private readonly IGameService _gameService_private;
    private readonly IGameListItemService _gameListItemService_private;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ProfileModel> _logger; 

    // --- Propiedades para las nuevas secciones ---
    public List<FriendViewModel> Friends { get; set; } = new();
    public QuickStatsViewModel QuickStats { get; set; } = new();
    public ActivityFeedItemViewModel? LatestActivity { get; set; }
    public List<GamePreviewDTO> RecentLikedGames { get; set; } = new();

    public ProfileModel(
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService,
        IGameService gameService,
        IGameListItemService gameListItemService,
        IModerationReportService moderationService,
        ILogger<ProfileModel> logger) 
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService no puede ser nulo.");
        _userManagerService_private = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _reviewService_private = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _listService_private = listService ?? throw new ArgumentNullException(nameof(listService), "IGameListService no puede ser nulo.");
        _gameTrackingService_private = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _gameService_private = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _gameListItemService_private = gameListItemService ?? throw new ArgumentNullException(nameof(gameListItemService), "IGameListItemService no puede ser nulo.");
        _moderationService = moderationService ?? throw new ArgumentNullException(nameof(moderationService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Profile";

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("OnGetAsync: userId es nulo o vacío. Redirigiendo a BadRequest.");
            TempData["StatusMessage"] = "Error: ID de usuario no proporcionado.";
            return BadRequest();
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga del perfil para el usuario '{ProfileUserId}'.", userId);

            var userExists = await LoadProfileHeaderData(
                userId,
                _authService,
                _userManagerService_private,
                _friendshipService,
                _reviewService_private,
                _listService_private,
                _gameTrackingService_private
            );

            if (!userExists)
            {
                _logger.LogWarning("OnGetAsync: Usuario con ID '{ProfileUserId}' no encontrado al cargar el perfil.", userId);
                TempData["StatusMessage"] = "Error: El perfil de usuario no fue encontrado.";
                return NotFound();
            }

            await LoadFriends(userId);

            await LoadQuickStats(userId);

            await LoadLatestActivity(userId);

            await LoadRecentLikedGames(userId);

            _logger.LogInformation("OnGetAsync: Carga del perfil para el usuario '{ProfileUserId}' completada exitosamente.", userId);
            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar el perfil para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar el perfil para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar el perfil para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Problema de conexión al cargar el perfil. Por favor, verifica tu internet.";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar el perfil para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            TempData["StatusMessage"] = "Ocurrió un error inesperado al cargar el perfil. Por favor, inténtalo de nuevo más tarde.";
            return RedirectToPage("/Error");
        }
    }

    private async Task LoadFriends(string userId)
    {
        try
        {
            _logger.LogDebug("LoadFriends: Cargando amigos para el usuario '{ProfileUserId}'.", userId);
            var friendDocs = await _friendshipService.GetFriendsAsync(userId);
            if (friendDocs != null && friendDocs.Any())
            {
                var friendTasks = friendDocs.Select(async f =>
                {
                    if (f == null) return null; // Salta amigos nulos en la lista

                    var friendId = (f.SenderId == userId) ? f.ReceiverId : f.SenderId;
                    if (string.IsNullOrWhiteSpace(friendId))
                    {
                        _logger.LogWarning("LoadFriends: ID de amigo nulo/vacío encontrado en el documento de amistad '{FriendshipId}' para el usuario '{ProfileUserId}'.", f.Id, userId);
                        return null;
                    }

                    try
                    {
                        var friendAuthUser = await _authService.SearchUserByIdAsync(friendId);
                        
                        if (friendAuthUser != null && friendAuthUser.status == "deleted")
                        {
                            return null;
                        }

                        var friendProfile = await _userManagerService_private.GetProfileAsync(friendId);
                        return new FriendViewModel
                        {
                            UserId = friendId,
                            Username = friendAuthUser?.Username ?? "Unknown User",
                            AvatarUrl = friendProfile?.AvatarUrl ?? "/images/default_avatar.png"
                        };
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "LoadFriends: HttpRequestException al obtener datos de amigo '{FriendId}' para el usuario '{ProfileUserId}'.", friendId, userId);
                        return null; 
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "LoadFriends: Error inesperado al obtener datos de amigo '{FriendId}' para el usuario '{ProfileUserId}'.", friendId, userId);
                        return null;
                    }
                }).Where(t => t != null); 
                
                Friends = (await Task.WhenAll(friendTasks)).Where(f => f != null).ToList()!; 
                _logger.LogInformation("LoadFriends: {Count} amigos cargados para el usuario '{ProfileUserId}'.", Friends.Count, userId);
            }
            else
            {
                _logger.LogInformation("LoadFriends: No se encontraron amigos para el usuario '{ProfileUserId}'.", userId);
            }
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "LoadFriends: HttpRequestException al cargar la lista de amigos para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            Friends = new List<FriendViewModel>(); 
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "LoadFriends: Error inesperado al cargar la lista de amigos para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            Friends = new List<FriendViewModel>();
        }
    }

    private async Task LoadQuickStats(string userId)
    {
        try
        {
            _logger.LogDebug("LoadQuickStats: Cargando estadísticas rápidas para el usuario '{ProfileUserId}'.", userId);
            var completedGameIds = await _gameTrackingService_private.GetGameIdsByStatusAsync(userId, "played");
            QuickStats.CompletedGamesCount = completedGameIds?.Count ?? 0;
            QuickStats.ReviewsCount = ProfileHeader.ReviewCount; 

            var userReviews = await _reviewService_private.GetFriendsReviewsAsync(new List<string> { userId });
            if (userReviews != null && userReviews.Any())
            {
                QuickStats.AverageRating = Math.Round(userReviews.Average(r => r.Rating), 1);
            }
            else { QuickStats.AverageRating = 0.0; }
            
            if (QuickStats.CompletedGamesCount > 20) QuickStats.PlayerRank = "Veteran";
            else if (QuickStats.CompletedGamesCount > 5) QuickStats.PlayerRank = "Explorer";
            else QuickStats.PlayerRank = "Newbie";

            _logger.LogInformation("LoadQuickStats: Estadísticas rápidas cargadas para el usuario '{ProfileUserId}'. Completed: {Completed}, Reviews: {Reviews}, AvgRating: {Rating}", 
                                   userId, QuickStats.CompletedGamesCount, QuickStats.ReviewsCount, QuickStats.AverageRating);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LoadQuickStats: HttpRequestException al cargar estadísticas rápidas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            QuickStats = new QuickStatsViewModel(); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadQuickStats: Error inesperado al cargar estadísticas rápidas para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            QuickStats = new QuickStatsViewModel();
        }
    }

    private async Task LoadLatestActivity(string userId)
    {
        try
        {
            _logger.LogDebug("LoadLatestActivity: Cargando la actividad más reciente para el usuario '{ProfileUserId}'.", userId);
            var reviewsTask = _reviewService_private.GetFriendsReviewsAsync(new List<string> { userId });
            var gameTrackingsTask = _gameTrackingService_private.GetTrackingsByUserAsync(userId);
            var gameListsTask = _listService_private.GetUserListsAsync(userId);

            await Task.WhenAll(reviewsTask, gameTrackingsTask, gameListsTask);

            var allActivityItems = new List<ActivityFeedItemViewModel>();

            var reviews = reviewsTask.Result ?? new List<ReviewDTO>();
            foreach (var r in reviews)
            {
                if (r == null || r.GameId == Guid.Empty || string.IsNullOrWhiteSpace(r.Id))
                {
                    _logger.LogWarning("LoadLatestActivity: Reseña inválida (nula, GameId vacío o Id nulo) encontrada para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    var gamePreview = await _gameService_private.GetGamePreviewByIdAsync(r.GameId);
                    if (gamePreview != null)
                    {
                        allActivityItems.Add(new ReviewActivityViewModel
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
                        _logger.LogWarning("LoadLatestActivity: GamePreview nulo para el juego '{GameId}' en la reseña '{ReviewId}'.", r.GameId, r.Id);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "LoadLatestActivity: HttpRequestException al obtener GamePreview para el juego '{GameId}' de la reseña '{ReviewId}'.", r.GameId, r.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoadLatestActivity: Error inesperado al mapear reseña '{ReviewId}' para el usuario '{ProfileUserId}'.", r.Id, userId);
                }
            }

            var gameTrackings = gameTrackingsTask.Result ?? new List<GameTrackingDto>();
            foreach (var gt in gameTrackings)
            {
                if (gt == null || string.IsNullOrWhiteSpace(gt.GameId) || string.IsNullOrWhiteSpace(gt.Id.ToString()))
                {
                    _logger.LogWarning("LoadLatestActivity: Seguimiento de juego inválido (nulo, GameId o Id nulo) encontrado para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    if (!Guid.TryParse(gt.GameId, out Guid parsedGameId))
                    {
                        _logger.LogWarning("LoadLatestActivity: GameId '{GameId}' en el seguimiento '{TrackingId}' no es un GUID válido para el usuario '{ProfileUserId}'.", gt.GameId, gt.Id, userId);
                        continue;
                    }
                    var gamePreview = await _gameService_private.GetGamePreviewByIdAsync(parsedGameId);
                    if (gamePreview != null)
                    {
                        allActivityItems.Add(new GameLogActivityViewModel
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
                        _logger.LogWarning("LoadLatestActivity: GamePreview nulo para el juego '{GameId}' en el seguimiento '{TrackingId}'.", parsedGameId, gt.Id);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "LoadLatestActivity: HttpRequestException al obtener GamePreview para el juego '{GameId}' del seguimiento '{TrackingId}'.", gt.GameId, gt.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoadLatestActivity: Error inesperado al mapear seguimiento '{TrackingId}' para el usuario '{ProfileUserId}'.", gt.Id, userId);
                }
            }

            var gameLists = gameListsTask.Result ?? new List<GameListDTO>();
            var gameListActivityTasks = new List<Task<GameListActivityViewModel>>();
            foreach (var gl in gameLists)
            {
                if (gl == null || string.IsNullOrWhiteSpace(gl.Id))
                {
                    _logger.LogWarning("LoadLatestActivity: Lista de juegos inválida (nula o Id nulo) encontrada para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    gameListActivityTasks.Add(Task.Run(async () =>
                    {
                        var listItems = await _gameListItemService_private.GetItemsByListIdAsync(gl.Id);
                        return new GameListActivityViewModel
                        {
                            Type = "GameList",
                            Timestamp = gl.CreatedAt,
                            UserId = gl.UserId,
                            Username = ProfileHeader.Username,
                            UserAvatarUrl = ProfileHeader.AvatarUrl,
                            ListId = gl.Id,
                            ListName = gl.Name,
                            Description = gl.Description,
                            ItemCount = listItems?.Count ?? 0
                        };
                    }));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoadLatestActivity: Error inesperado al mapear lista '{ListId}' para el usuario '{ProfileUserId}'.", gl.Id, userId);
                }
            }
            allActivityItems.AddRange(await Task.WhenAll(gameListActivityTasks));

            LatestActivity = allActivityItems.OrderByDescending(a => a.Timestamp).FirstOrDefault();
            _logger.LogInformation("LoadLatestActivity: Actividad más reciente cargada para el usuario '{ProfileUserId}'.", userId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LoadLatestActivity: HttpRequestException general al cargar la actividad más reciente para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            LatestActivity = null; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadLatestActivity: Error inesperado al cargar la actividad más reciente para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            LatestActivity = null;
        }
    }

    private async Task LoadRecentLikedGames(string userId)
    {
        try
        {
            _logger.LogDebug("LoadRecentLikedGames: Cargando juegos recientes a los que le ha gustado al usuario '{ProfileUserId}'.", userId);
            var likedGameIdsStrings = await _gameTrackingService_private.GetLikedGameIdsAsync(userId);
            var likedGameIds = likedGameIdsStrings?.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList() ?? new List<Guid>();
            
            var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
            foreach (var gameId in likedGameIds.Take(6))
            {
                if (gameId == Guid.Empty)
                {
                    _logger.LogWarning("LoadRecentLikedGames: Se encontró un GameId vacío en la lista de IDs de juegos gustados para el usuario '{ProfileUserId}'. Saltando.", userId);
                    continue;
                }
                try
                {
                    gamePreviewTasks.Add(_gameService_private.GetGamePreviewByIdAsync(gameId));
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "LoadRecentLikedGames: HttpRequestException al obtener GamePreview para el juego '{GameId}' gustado por el usuario '{ProfileUserId}'.", gameId, userId);
                    gamePreviewTasks.Add(Task.FromResult<GamePreviewDTO?>(null)); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "LoadRecentLikedGames: Error inesperado al obtener GamePreview para el juego '{GameId}' gustado por el usuario '{ProfileUserId}'.", gameId, userId);
                    gamePreviewTasks.Add(Task.FromResult<GamePreviewDTO?>(null));
                }
            }
            RecentLikedGames = (await Task.WhenAll(gamePreviewTasks)).Where(gp => gp != null).ToList()!;
            _logger.LogInformation("LoadRecentLikedGames: {Count} juegos gustados recientes cargados para el usuario '{ProfileUserId}'.", RecentLikedGames.Count, userId);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "LoadRecentLikedGames: HttpRequestException general al cargar juegos gustados recientes para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            RecentLikedGames = new List<GamePreviewDTO>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LoadRecentLikedGames: Error inesperado al cargar juegos gustados recientes para el usuario '{ProfileUserId}'. Mensaje: {Message}", userId, ex.Message);
            RecentLikedGames = new List<GamePreviewDTO>();
        }
    }

   

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostSendRequestAsync(string profileUserId)
    {
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