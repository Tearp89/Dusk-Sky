using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; 

public class GameDetailsModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IReviewService _reviewService;
    private readonly IUserManagerService _userService;
    private readonly IAuthService _authService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;
    private readonly ILogger<GameDetailsModel> _logger; 

    public GameDetailsModel(
        IGameService gameService,
        IReviewService reviewService,
        IUserManagerService userService,
        IAuthService authService,
        IGameTrackingService gameTrackingService,
        IGameListService gameListService,
        IGameListItemService gameListItemService,
        ILogger<GameDetailsModel> logger) 
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _userService = userService ?? throw new ArgumentNullException(nameof(userService), "IUserManagerService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _gameTrackingService = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _gameListService = gameListService ?? throw new ArgumentNullException(nameof(gameListService), "IGameListService no puede ser nulo.");
        _gameListItemService = gameListItemService ?? throw new ArgumentNullException(nameof(gameListItemService), "IGameListItemService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
    }

    [BindProperty(SupportsGet = true)]
    public Guid gameId { get; set; }
    public GameDetailsDTO? Game { get; set; }
    public GamePreviewDTO? GamePreview { get; set; }
    public List<ReviewFullDto> Reviews { get; set; } = new();
    public GameTrackingDto? Tracking { get; set; }
    public List<GameListDTO> UserLists { get; set; } = new();
    public bool IsWatched => Tracking?.Status == "played";
    public bool IsInWatchlist => Tracking?.Status == "backlog";
    public bool IsLiked => Tracking?.Liked == true;

    public async Task<IActionResult> OnGetAsync()
    {
        // ✅ Validar que gameId no sea Guid.Empty al inicio
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnGetAsync: gameId es Guid.Empty. Redirigiendo a NotFound."); 
            TempData["ErrorMessage"] = "ID de juego no proporcionado.";
            return NotFound();
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("OnGetAsync: Cargando detalles del juego '{GameId}' para el usuario '{UserId}'.", gameId, userId ?? "Anónimo"); 

            Game = await _gameService.GetGameByIdAsync(gameId);
            GamePreview = await _gameService.GetGamePreviewByIdAsync(gameId);

            if (Game == null)
            {
                _logger.LogWarning("OnGetAsync: Juego con ID {GameId} no encontrado.", gameId); 
                TempData["ErrorMessage"] = "El juego solicitado no fue encontrado.";
                return NotFound();
            }

            var backgroundImageUrl = !string.IsNullOrEmpty(Game.RandomScreenshot)
                                         ? Game.RandomScreenshot
                                         : (string.IsNullOrEmpty(Game.HeaderUrl) ? "/Images/default-game-background.png" : Game.HeaderUrl);
            ViewData["BackgroundImage"] = Url.Content(backgroundImageUrl);
            ViewData["UseBlurEffect"] = true;

            if (!string.IsNullOrEmpty(userId))
            {
                UserLists = await _gameListService.GetUserListsAsync(userId) ?? new List<GameListDTO>();
                _logger.LogDebug("OnGetAsync: {Count} listas de usuario cargadas para el usuario '{UserId}'.", UserLists.Count, userId); 

                Tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, gameId.ToString());
                _logger.LogDebug("OnGetAsync: Tracking cargado para el usuario '{UserId}' y juego '{GameId}'. Estado: {Status}, Gusta: {Liked}", userId, gameId, Tracking?.Status, Tracking?.Liked); 
            }
            else
            {
                _logger.LogInformation("OnGetAsync: Usuario no autenticado. No se cargaron listas ni seguimiento del usuario."); 
            }

            var allReviews = await _reviewService.GetTopReviewsByGameAsync(gameId.ToString());
            if (allReviews != null)
            {
                var reviewTasks = allReviews.Select(review => EnrichReviewFullDtoAsync(review, Game.Title, Game.HeaderUrl, userId));
                Reviews = (await Task.WhenAll(reviewTasks)).Where(r => r != null).OrderByDescending(r => r.CreatedAt).ToList();
                _logger.LogInformation("OnGetAsync: {Count} reseñas cargadas y enriquecidas para el juego '{GameId}'.", Reviews.Count, gameId); 
            }
            else
            {
                _logger.LogInformation("OnGetAsync: No se encontraron reseñas para el juego '{GameId}'.", gameId); 
            }

            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar detalles del juego '{GameId}'. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = $"Error de argumento: {ex.Message}";
            return Page();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para operaciones inválidas
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar detalles del juego '{GameId}'. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = $"Operación inválida: {ex.Message}";
            return Page();
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con servicios externos
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar detalles del juego '{GameId}'. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al cargar el juego: {ex.Message}";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar la página de detalles del juego con ID {GameId}. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = "Ocurrió un error al cargar los detalles del juego. Por favor, inténtalo de nuevo más tarde.";
            return Page();
        }
    }

    private async Task<ReviewFullDto> EnrichReviewFullDtoAsync(ReviewDTO review, string gameTitle, string gameImageUrl, string? currentUserId)
    {
        if (review == null)
        {
            _logger.LogWarning("EnrichReviewFullDtoAsync: Se intentó enriquecer un ReviewDTO nulo."); 
            return new ReviewFullDto { /* valores por defecto o nulos controlados */ };
        }
        if (string.IsNullOrEmpty(review.UserId))
        {
            _logger.LogWarning("EnrichReviewFullDtoAsync: ReviewDTO '{ReviewId}' tiene un UserId nulo o vacío.", review.Id); 
        }
        if (review.GameId == Guid.Empty)
        {
            _logger.LogWarning("EnrichReviewFullDtoAsync: ReviewDTO '{ReviewId}' tiene un GameId vacío.", review.Id); 
        }

        try
        {
            var profileTask = _userService.GetProfileAsync(review.UserId);
            var accountTask = _authService.SearchUserByIdAsync(review.UserId);


            await Task.WhenAll(profileTask, accountTask);

            var profile = profileTask.Result;
            var account = accountTask.Result;

            if (account == null || !string.Equals(account.status, "active", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Review '{ReviewId}' omitida porque el usuario '{UserId}' no está activo.", review.Id, review.UserId);
                return null;
            }

            return new ReviewFullDto
            {
                Id = review.Id,
                GameId = review.GameId.ToString(),
                GameTitle = gameTitle,
                GameImageUrl = gameImageUrl,
                UserId = review.UserId,
                UserName = account?.Username ?? "Unknown User", 
                ProfileImageUrl = profile?.AvatarUrl ?? "/Images/noImage.png", 
                Content = review.Content,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                Likes = review.Likes,
                LikedBy = review.LikedBy ?? new List<string>(), 
                UserLiked = currentUserId != null && (review.LikedBy?.Contains(currentUserId) ?? false) 
            };
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "EnrichReviewFullDtoAsync: HttpRequestException al obtener perfil/cuenta para la reseña '{ReviewId}'. Mensaje: {Message}", review.Id, ex.Message); 
            return new ReviewFullDto
            {
                Id = review.Id,
                GameId = review.GameId.ToString(),
                GameTitle = gameTitle,
                GameImageUrl = gameImageUrl,
                UserId = review.UserId,
                UserName = "Error al Cargar Usuario",
                ProfileImageUrl = "/Images/error.png",
                Content = review.Content,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                Likes = review.Likes,
                LikedBy = review.LikedBy ?? new List<string>(),
                UserLiked = currentUserId != null && (review.LikedBy?.Contains(currentUserId) ?? false)
            };
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "EnrichReviewFullDtoAsync: Error inesperado al enriquecer la reseña '{ReviewId}'. Mensaje: {Message}", review.Id, ex.Message); 
            return new ReviewFullDto
            {
                Id = review.Id,
                GameId = review.GameId.ToString(),
                GameTitle = gameTitle,
                GameImageUrl = gameImageUrl,
                UserId = review.UserId,
                UserName = "Error General",
                ProfileImageUrl = "/Images/error.png",
                Content = review.Content,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                Likes = review.Likes,
                LikedBy = review.LikedBy ?? new List<string>(),
                UserLiked = currentUserId != null && (review.LikedBy?.Contains(currentUserId) ?? false)
            };
        }
    }

    public async Task<IActionResult> OnPostToggleTrackingAsync(string trackingType)
    {
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostToggleTrackingAsync: gameId es Guid.Empty. Redirigiendo a la misma página.");
            TempData["ErrorMessage"] = "ID de juego no válido para el seguimiento.";
            return RedirectToPage(new { gameId });
        }
        if (string.IsNullOrEmpty(trackingType))
        {
            _logger.LogWarning("OnPostToggleTrackingAsync: trackingType es nulo o vacío.");
            TempData["ErrorMessage"] = "Tipo de seguimiento no especificado.";
            return RedirectToPage(new { gameId });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostToggleTrackingAsync: Usuario no autenticado intentando cambiar tracking para el juego '{GameId}'.", gameId); 
                TempData["ErrorMessage"] = "Debes iniciar sesión para actualizar el seguimiento.";
                return Unauthorized();
            }

            Tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, gameId.ToString()) ?? new GameTrackingDto
            {
                UserId = userId,
                GameId = gameId.ToString(),
                Liked = false,
                Status = null
            };

            switch (trackingType)
            {
                case "watch":
                    Tracking.Status = Tracking.Status == "played" ? null : "played";
                    break;
                case "watchlist":
                    Tracking.Status = Tracking.Status == "backlog" ? null : "backlog";
                    break;
                case "like":
                    Tracking.Liked = !Tracking.Liked;
                    break;
                default:
                    _logger.LogWarning("OnPostToggleTrackingAsync: Tipo de seguimiento '{TrackingType}' no reconocido para el juego '{GameId}'.", trackingType, gameId); 
                    TempData["ErrorMessage"] = "Tipo de seguimiento no reconocido.";
                    return RedirectToPage(new { gameId });
            }

            if (Tracking.Id == Guid.Empty)
            {
                await _gameTrackingService.CreateAsync(Tracking);
                _logger.LogInformation("OnPostToggleTrackingAsync: Nuevo seguimiento creado para el juego '{GameId}' por el usuario '{UserId}'. Estado: {Status}, Gusta: {Liked}", gameId, userId, Tracking.Status, Tracking.Liked); 
            }
            else
            {
                await _gameTrackingService.UpdateAsync(Tracking.Id, Tracking);
                _logger.LogInformation("OnPostToggleTrackingAsync: Seguimiento actualizado para el juego '{GameId}' por el usuario '{UserId}'. Estado: {Status}, Gusta: {Liked}", gameId, userId, Tracking.Status, Tracking.Liked); 
            }

            TempData["SuccessMessage"] = "Estado del juego actualizado correctamente.";
            return RedirectToPage(new { gameId });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostToggleTrackingAsync: HttpRequestException al actualizar tracking para el juego '{GameId}'. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al actualizar el estado del juego: {ex.Message}";
            return RedirectToPage(new { gameId });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostToggleTrackingAsync: Error inesperado al actualizar el seguimiento para el juego '{GameId}'. Mensaje: {Message}", gameId, ex.Message); 
            TempData["ErrorMessage"] = "No se pudo actualizar el estado del juego. Inténtalo de nuevo.";
            return RedirectToPage(new { gameId });
        }
    }

    public async Task<IActionResult> OnPostAddGameToListAsync(string ListId, Guid GameId)
    {
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnPostAddGameToListAsync: ID de lista nulo o vacío proporcionado."); 
            return new JsonResult(new { success = false, message = "ID de lista no válido." }) { StatusCode = 400 };
        }
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameToListAsync: ID de juego nulo (Guid.Empty) proporcionado para la lista '{ListId}'.", ListId); 
            return new JsonResult(new { success = false, message = "ID de juego no válido." }) { StatusCode = 400 };
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostAddGameToListAsync: Usuario no autenticado intentando añadir juego '{GameId}' a la lista '{ListId}'.", GameId, ListId); 
                return new JsonResult(new { success = false, message = "Usuario no autenticado." }) { StatusCode = 401 };
            }

            var alreadyExists = await _gameListItemService.ExistsAsync(ListId, GameId);
            if (alreadyExists)
            {
                _logger.LogInformation("OnPostAddGameToListAsync: Juego '{GameId}' ya existe en la lista '{ListId}' para el usuario '{UserId}'.", GameId, ListId, userId); 
                return new JsonResult(new { success = false, message = "El juego ya está en esta lista." });
            }

            var dto = new GameListItemDTO
            {
                Id = Guid.NewGuid().ToString(),
                GameId = GameId,
                ListId = ListId
            };

            var success = await _gameListItemService.AddItemAsync(dto);
            if (success)
            {
                _logger.LogInformation("OnPostAddGameToListAsync: Juego '{GameId}' añadido exitosamente a la lista '{ListId}' por el usuario '{UserId}'.", GameId, ListId, userId); 
                return new JsonResult(new { success = true, message = "Juego añadido a la lista correctamente." }); 
            }
            else
            {
                _logger.LogError("OnPostAddGameToListAsync: Falló al añadir el juego '{GameId}' a la lista '{ListId}' para el usuario '{UserId}'. El servicio AddItemAsync devolvió false.", GameId, ListId, userId); 
                return new JsonResult(new { success = false, message = "No se pudo agregar el juego a la lista." });
            }
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostAddGameToListAsync: HttpRequestException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", GameId, ListId, ex.Message); 
            return new JsonResult(new { success = false, message = $"Problema de conexión al agregar el juego: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAddGameToListAsync: Error inesperado al añadir el juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", GameId, ListId, ex.Message); 
            return new JsonResult(new { success = false, message = "Ocurrió un error en el servidor al intentar agregar el juego a la lista." }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostLogReviewWithTrackingAsync(Guid GameId, string Content, double Rating, DateTime? WatchedOn, bool PlayedBefore, bool Like)
    {
        // ✅ Validar parámetros de entrada al inicio
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostLogReviewWithTrackingAsync: GameId es Guid.Empty. No se puede procesar la reseña."); 
            return new JsonResult(new { success = false, message = "ID de juego no válido." }) { StatusCode = 400 };
        }
        if (string.IsNullOrWhiteSpace(Content))
        {
            _logger.LogWarning("OnPostLogReviewWithTrackingAsync: El contenido de la reseña está vacío o nulo para el juego '{GameId}'.", GameId); 
            return new JsonResult(new { success = false, message = "El contenido de la reseña no puede estar vacío." }) { StatusCode = 400 };
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostLogReviewWithTrackingAsync: Usuario no autenticado intentando registrar reseña para el juego '{GameId}'.", GameId); 
                return new JsonResult(new { success = false, message = "Usuario no autenticado." }) { StatusCode = 401 };
            }

            var review = new ReviewDTO
            {
                GameId = GameId,
                UserId = userId,
                Content = Content,
                Rating = Rating,
                CreatedAt = DateTime.UtcNow
            };
            await _reviewService.CreateReviewAsync(review);
            _logger.LogInformation("OnPostLogReviewWithTrackingAsync: Reseña creada para el juego '{GameId}' por el usuario '{UserId}'.", GameId, userId); 


            var tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, GameId.ToString()) ?? new GameTrackingDto
            {
                UserId = userId,
                GameId = GameId.ToString()
            };

            if (WatchedOn.HasValue || PlayedBefore)
            {
                tracking.Status = "played";
                tracking.Liked = Like;
            }
            if (tracking.Id == Guid.Empty)
            {
                await _gameTrackingService.CreateAsync(tracking);
                _logger.LogInformation("OnPostLogReviewWithTrackingAsync: Nuevo tracking creado para el juego '{GameId}' por el usuario '{UserId}'.", GameId, userId); 
            }
            else
            {
                await _gameTrackingService.UpdateAsync(tracking.Id, tracking);
                _logger.LogInformation("OnPostLogReviewWithTrackingAsync: Tracking existente actualizado para el juego '{GameId}' por el usuario '{UserId}'.", GameId, userId); 
            }
            TempData["SuccessMessage"] = "Review registrada exitosamente";

            return new JsonResult(new { success = true, message = "Reseña y seguimiento guardados exitosamente." });

        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostLogReviewWithTrackingAsync: HttpRequestException al registrar reseña/tracking para el juego '{GameId}'. Mensaje: {Message}", GameId, ex.Message); 
            return new JsonResult(new { success = false, message = $"Problema de conexión al registrar la reseña: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostLogReviewWithTrackingAsync: Error inesperado al registrar reseña/tracking para el juego '{GameId}'. Mensaje: {Message}", GameId, ex.Message); 
            return new JsonResult(new { success = false, message = "Ocurrió un error en el servidor al intentar registrar la reseña." }) { StatusCode = 500 };
        }
    }
}