using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using
using System.Net.Http; // ✅ Añadir para HttpRequestException

[Authorize]
public class ReviewDetailsModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly ICommentService _commentService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameListItemService _gameListItemService;
    private readonly IGameListService _gameListService;
    private readonly IModerationReportService _moderationReportService;
    private readonly ILogger<ReviewDetailsModel> _logger; // ✅ Declaración del logger

    public ReviewDetailsModel(
        IReviewService reviewService,
        ICommentService commentService,
        IUserManagerService userService,
        IGameService gameService,
        IAuthService authService,
        IGameTrackingService gameTrackingService,
        IGameListItemService gameListItemService,
        IGameListService gameListService,
        IModerationReportService moderationReportService,
        ILogger<ReviewDetailsModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para todos los servicios y el logger inyectados en el constructor
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _commentService = commentService ?? throw new ArgumentNullException(nameof(commentService), "ICommentService no puede ser nulo.");
        _userService = userService ?? throw new ArgumentNullException(nameof(userService), "IUserManagerService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _gameTrackingService = gameTrackingService ?? throw new ArgumentNullException(nameof(gameTrackingService), "IGameTrackingService no puede ser nulo.");
        _gameListItemService = gameListItemService ?? throw new ArgumentNullException(nameof(gameListItemService), "IGameListItemService no puede ser nulo.");
        _gameListService = gameListService ?? throw new ArgumentNullException(nameof(gameListService), "IGameListService no puede ser nulo.");
        _moderationReportService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "IModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
    }

    [BindProperty(SupportsGet = true)]
    public string ReviewId { get; set; } = string.Empty;
    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; } // ✅ Propiedad para mensajes de error

    public ReviewWithUserDto? Review { get; set; }
    public List<CommentViewModel> Comments { get; set; } = new();
    public GameTrackingDto Tracking { get; set; } = new();
    public GamePreviewDTO GamePreview { get; set; } = new();
    public ReviewDTO ReviewDTO { get; set; } = new();
    public List<ReviewWithGameDto> UserReviews { get; set; } = new();
    public List<GameListDTO> UserLists { get; set; } = new();

    public bool IsWatched => Tracking?.Status == "played";
    public bool IsInWatchlist => Tracking?.Status == "backlog";
    public bool IsLiked => Tracking?.Liked == true;
    public string UserId { get; set; } = string.Empty; // ✅ Inicializar para evitar nulos
    public List<string> UserIds { get; set; } = new();

    [BindProperty]
    public ReportReviewInputModel ReportInput { get; set; } = new();


    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty; // ✅ Asegurar que UserId no sea nulo
            _logger.LogInformation("OnGetAsync: Iniciando carga de detalles de reseña '{ReviewId}' para el usuario '{UserId}'.", ReviewId, UserId); // ✅ Registro de información

            // ✅ Validar que ReviewId no sea nulo o vacío
            if (string.IsNullOrWhiteSpace(ReviewId))
            {
                _logger.LogWarning("OnGetAsync: ReviewId es nulo o vacío. Redirigiendo a /Error."); // ✅ Registro de advertencia
                ErrorMessage = "ID de la reseña no proporcionado.";
                return RedirectToPage("/Error");
            }

            var reviewDto = await _reviewService.GetReviewByIdAsync(ReviewId);
            // ✅ Validar que la review exista
            if (reviewDto is null)
            {
                _logger.LogWarning("OnGetAsync: Reseña con ID '{ReviewId}' no encontrada.", ReviewId); // ✅ Registro de advertencia
                ErrorMessage = "Reseña no encontrada.";
                return NotFound();
            }

            // ✅ Validar que reviewDto.UserId no sea nulo o vacío antes de usarlo
            if (string.IsNullOrWhiteSpace(reviewDto.UserId))
            {
                _logger.LogError("OnGetAsync: Reseña con ID '{ReviewId}' tiene un UserId nulo o vacío.", ReviewId); // ✅ Registro de error
                ErrorMessage = "ID de usuario de la reseña inválido.";
                return NotFound();
            }

            // Cargar datos del usuario autor de la review
            UserProfileDTO? userProfile = null;
            UserSearchResultDto? userAccount = null;
            try
            {
                userProfile = await _userService.GetProfileAsync(reviewDto.UserId);
                userAccount = await _authService.SearchUserByIdAsync(reviewDto.UserId);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar perfil/cuenta para el usuario '{ReviewUserId}' de la reseña '{ReviewId}'.", reviewDto.UserId, ReviewId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar perfil/cuenta para el usuario '{ReviewUserId}' de la reseña '{ReviewId}'.", reviewDto.UserId, ReviewId);
            }


            // ✅ Validar que reviewDto.GameId no sea Guid.Empty
            if (reviewDto.GameId == Guid.Empty)
            {
                _logger.LogError("OnGetAsync: Reseña con ID '{ReviewId}' tiene un GameId vacío.", ReviewId); // ✅ Registro de error
                ErrorMessage = "ID de juego de la reseña inválido.";
                return NotFound();
            }

            // Cargar datos del juego asociado
            var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);
            // ✅ Validar que el juego exista
            if (game == null)
            {
                _logger.LogWarning("OnGetAsync: Juego asociado a la reseña '{ReviewId}' con GameId '{GameId}' no encontrado.", ReviewId, reviewDto.GameId); // ✅ Registro de advertencia
                ErrorMessage = "Juego asociado a la reseña no encontrado.";
                return NotFound();
            }

            GamePreview = new GamePreviewDTO
            {
                Id = game.Id,
                Title = game.Title,
                HeaderUrl = game.HeaderUrl
            };

            Review = new ReviewWithUserDto
            {
                Id = reviewDto.Id,
                GameId = reviewDto.GameId,
                UserId = reviewDto.UserId,
                Content = reviewDto.Content,
                Likes = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt,
                Rating = reviewDto.Rating,
                LikedBy = reviewDto.LikedBy ?? new List<string>(),
                UserLiked = !string.IsNullOrEmpty(UserId) && (reviewDto.LikedBy?.Contains(UserId) ?? false),
                UserName = userAccount?.Username ?? "Usuario desconocido",
                ProfileImageUrl = userProfile?.AvatarUrl ?? "/images/noavatar.png",
                GameImageUrl = game?.HeaderUrl ?? "/images/noimage.png"
            };
            _logger.LogDebug("OnGetAsync: DTO de reseña enriquecido para la reseña '{ReviewId}'.", ReviewId); // ✅ Registro de depuración

            Tracking = await _gameTrackingService.GetByUserAndGameAsync(UserId, Review.GameId.ToString());
            Tracking ??= new GameTrackingDto(); // ✅ Asegurar que Tracking no sea nulo
            _logger.LogDebug("OnGetAsync: Tracking cargado para el juego '{GameId}' y usuario '{UserId}'.", Review.GameId, UserId); // ✅ Registro de depuración

            var commentList = await _commentService.GetCommentsByReviewIdAsync(ReviewId);
            Comments = new List<CommentViewModel>(); // ✅ Inicializar antes de añadir
            if (commentList != null)
            {
                foreach (var comment in commentList)
                {
                    if (comment == null || string.IsNullOrWhiteSpace(comment.AuthorId))
                    {
                        _logger.LogWarning("OnGetAsync: Comentario nulo o con AuthorId vacío encontrado para la reseña '{ReviewId}'.", ReviewId);
                        continue;
                    }
                    try
                    {
                        var author = await _userService.GetProfileAsync(comment.AuthorId);
                        var authorUser = await _authService.SearchUserByIdAsync(comment.AuthorId);
                        Comments.Add(new CommentViewModel
                        {
                            UserName = authorUser?.Username ?? "Anónimo",
                            UserAvatarUrl = author?.AvatarUrl ?? "/images/noavatar.png",
                            Content = comment.Text,
                            CreatedAt = comment.CreatedAt
                        });
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar detalles del autor del comentario '{CommentId}' para la reseña '{ReviewId}'.", comment.Id, ReviewId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar detalles del autor del comentario '{CommentId}' para la reseña '{ReviewId}'.", comment.Id, ReviewId);
                    }
                }
                _logger.LogInformation("OnGetAsync: {Count} comentarios cargados para la reseña '{ReviewId}'.", Comments.Count, ReviewId); // ✅ Registro de información
            }
            else
            {
                _logger.LogInformation("OnGetAsync: No se encontraron comentarios para la reseña '{ReviewId}'.", ReviewId); // ✅ Registro de información
            }

            UserReviews = new List<ReviewWithGameDto>();
            UserIds.Add(UserId); // Añadir el ID del propio usuario a UserIds para GetFriendsReviewsAsync si es el caso

            var userReviews = await _reviewService.GetFriendsReviewsAsync(UserIds);
            if (userReviews != null)
            {
                foreach (var review in userReviews)
                {
                    if (review == null || review.GameId == Guid.Empty)
                    {
                        _logger.LogWarning("OnGetAsync: Reseña de usuario inválida (nula o GameId vacío) encontrada para el usuario '{UserId}'.", UserId);
                        continue;
                    }
                    try
                    {
                        var gameReview = await _gameService.GetGamePreviewByIdAsync(review.GameId);
                        if (gameReview != null)
                        {
                            UserReviews.Add(new ReviewWithGameDto
                            {
                                ReviewId = review.Id,
                                GameId = review.GameId.ToString(),
                                GameTitle = gameReview.Title,
                                GameImageUrl = gameReview.HeaderUrl
                            });
                        }
                        else
                        {
                            _logger.LogWarning("OnGetAsync: No se encontró la vista previa del juego '{GameId}' para la reseña de usuario '{ReviewId}'.", review.GameId, review.Id);
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar vista previa del juego para la reseña de usuario '{ReviewId}'.", review.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar vista previa del juego para la reseña de usuario '{ReviewId}'.", review.Id);
                    }
                }
                _logger.LogInformation("OnGetAsync: {Count} reseñas de usuario cargadas.", UserReviews.Count); // ✅ Registro de información
            }
            else
            {
                _logger.LogInformation("OnGetAsync: No se encontraron reseñas de usuario para el usuario '{UserId}'.", UserId); // ✅ Registro de información
            }

            UserLists = await _gameListService.GetUserListsAsync(UserId);
            UserLists ??= new List<GameListDTO>(); // ✅ Asegurar que UserLists no sea nulo
            _logger.LogInformation("OnGetAsync: {Count} listas de usuario cargadas para el usuario '{UserId}'.", UserLists.Count, UserId); // ✅ Registro de información
        }
        catch (ArgumentException ex) // ✅ Catch específico para ArgumentException
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar detalles de la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            ErrorMessage = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar detalles de la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            ErrorMessage = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red generales
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException general al cargar detalles de la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            ErrorMessage = $"Problema de conexión al cargar la reseña: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar los detalles de la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            ErrorMessage = $"Ocurrió un error al cargar los detalles de la reseña: {ex.Message}";
            return RedirectToPage("/Error");
        }
        return Page();
    }


    public async Task<IActionResult> OnPostAgregarComentarioAsync(string reviewId, string nuevoComentario)
    {
        // ✅ Validar reviewId y nuevoComentario al inicio
        if (string.IsNullOrWhiteSpace(reviewId))
        {
            _logger.LogWarning("OnPostAgregarComentarioAsync: ID de reseña nulo o vacío para el comentario.");
            TempData["ErrorMessage"] = "ID de reseña no válido para el comentario.";
            return RedirectToPage("/Reviews/Details", new { reviewId });
        }
        if (string.IsNullOrWhiteSpace(nuevoComentario))
        {
            _logger.LogWarning("OnPostAgregarComentarioAsync: Contenido del comentario vacío para la reseña '{ReviewId}'.", reviewId);
            TempData["ErrorMessage"] = "El comentario no puede estar vacío.";
            return RedirectToPage("/Reviews/Details", new { reviewId });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ✅ Validar que userId no sea nulo o vacío
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostAgregarComentarioAsync: Usuario no autenticado intentando comentar en la reseña '{ReviewId}'.", reviewId);
                TempData["ErrorMessage"] = "Usuario no autenticado para comentar.";
                return Unauthorized();
            }

            var nuevo = new CommentDTO
            {
                ReviewId = reviewId,
                AuthorId = userId,
                Text = nuevoComentario,
                CreatedAt = DateTime.UtcNow,
                Status = CommentStatus.Visible
            };

            await _commentService.CreateCommentAsync(nuevo);
            _logger.LogInformation("OnPostAgregarComentarioAsync: Comentario agregado exitosamente por el usuario '{UserId}' en la reseña '{ReviewId}'.", userId, reviewId);
            TempData["SuccessMessage"] = "Comentario agregado exitosamente.";
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostAgregarComentarioAsync: HttpRequestException al agregar comentario en la reseña '{ReviewId}'. Mensaje: {Message}", reviewId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al agregar el comentario: {ex.Message}";
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostAgregarComentarioAsync: Error inesperado al agregar comentario en la reseña '{ReviewId}'. Mensaje: {Message}", reviewId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al agregar el comentario: {ex.Message}";
        }
        return RedirectToPage("/Reviews/Details", new { reviewId });
    }

    public async Task<IActionResult> OnPostToggleTrackingAsync(string reviewId, string trackingType)
    {
        // ✅ Validar reviewId y trackingType al inicio
        if (string.IsNullOrWhiteSpace(reviewId))
        {
            _logger.LogWarning("OnPostToggleTrackingAsync: ID de reseña nulo o vacío para el seguimiento.");
            TempData["ErrorMessage"] = "ID de reseña no válido para el seguimiento.";
            return RedirectToPage("/Reviews/Details", new { reviewId });
        }
        if (string.IsNullOrWhiteSpace(trackingType))
        {
            _logger.LogWarning("OnPostToggleTrackingAsync: Tipo de seguimiento nulo o vacío para la reseña '{ReviewId}'.", reviewId);
            TempData["ErrorMessage"] = "Tipo de seguimiento no especificado.";
            return RedirectToPage("/Reviews/Details", new { reviewId });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // ✅ Validar que userId no sea nulo o vacío
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostToggleTrackingAsync: Usuario no autenticado para el seguimiento en la reseña '{ReviewId}'.", reviewId);
                TempData["ErrorMessage"] = "Usuario no autenticado para el seguimiento.";
                return Unauthorized();
            }

            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            // ✅ Validar que la review exista
            if (review is null)
            {
                _logger.LogWarning("OnPostToggleTrackingAsync: Reseña con ID '{ReviewId}' no encontrada para el seguimiento.", reviewId);
                TempData["ErrorMessage"] = "Reseña no encontrada para el seguimiento.";
                return RedirectToPage("/Reviews/Details", new { reviewId });
            }
            // ✅ Validar que review.GameId no sea Guid.Empty
            if (review.GameId == Guid.Empty)
            {
                _logger.LogError("OnPostToggleTrackingAsync: Reseña con ID '{ReviewId}' tiene un GameId vacío.", reviewId);
                TempData["ErrorMessage"] = "ID de juego asociado a la reseña inválido.";
                return RedirectToPage("/Reviews/Details", new { reviewId });
            }

            var currentTracking = await _gameTrackingService.GetByUserAndGameAsync(userId, review.GameId.ToString());
            currentTracking ??= new GameTrackingDto
            {
                UserId = userId,
                GameId = review.GameId.ToString(),
                Liked = false,
                Status = null
            };
            _logger.LogDebug("OnPostToggleTrackingAsync: Tracking actual para el juego '{GameId}' y usuario '{UserId}' cargado. ID Tracking: {TrackingId}", review.GameId, userId, currentTracking.Id);

            switch (trackingType)
            {
                case "watch":
                    currentTracking.Status = currentTracking.Status == "played" ? null : "played";
                    break;
                case "watchlist":
                    currentTracking.Status = currentTracking.Status == "backlog" ? null : "backlog";
                    break;
                case "like":
                    currentTracking.Liked = !currentTracking.Liked;
                    break;
                default:
                    _logger.LogWarning("OnPostToggleTrackingAsync: Tipo de seguimiento '{TrackingType}' no reconocido para la reseña '{ReviewId}'.", trackingType, reviewId);
                    TempData["ErrorMessage"] = "Tipo de seguimiento no reconocido.";
                    return RedirectToPage("/Reviews/Details", new { reviewId });
            }

            if (currentTracking.Id == Guid.Empty)
            {
                await _gameTrackingService.CreateAsync(currentTracking);
                _logger.LogInformation("OnPostToggleTrackingAsync: Nuevo seguimiento creado para el juego '{GameId}' por el usuario '{UserId}'.", review.GameId, userId);
            }
            else
            {
                await _gameTrackingService.UpdateAsync(currentTracking.Id, currentTracking);
                _logger.LogInformation("OnPostToggleTrackingAsync: Seguimiento actualizado para el juego '{GameId}' por el usuario '{UserId}'.", review.GameId, userId);
            }

            TempData["SuccessMessage"] = "Seguimiento actualizado exitosamente.";
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostToggleTrackingAsync: HttpRequestException al actualizar el seguimiento para la reseña '{ReviewId}'. Mensaje: {Message}", reviewId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al actualizar el seguimiento: {ex.Message}";
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostToggleTrackingAsync: Error inesperado al actualizar el seguimiento para la reseña '{ReviewId}'. Mensaje: {Message}", reviewId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al actualizar el seguimiento: {ex.Message}";
        }
        return RedirectToPage("/Reviews/Details", new { reviewId });
    }

    public async Task<IActionResult> OnPostToggleTrackingAjaxAsync([FromBody] TrackingRequest request)
    {
        // ✅ Validar request y sus propiedades al inicio
        if (request == null || string.IsNullOrWhiteSpace(request.ReviewId) || string.IsNullOrWhiteSpace(request.Type))
        {
            _logger.LogWarning("OnPostToggleTrackingAjaxAsync: Datos de solicitud de tracking inválidos (ReviewId o Type nulo/vacío).");
            return new JsonResult(new { success = false, message = "Datos de solicitud inválidos." }) { StatusCode = 400 };
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostToggleTrackingAjaxAsync: Usuario no autenticado para el tracking AJAX en reseña '{ReviewId}'.", request.ReviewId);
                return new JsonResult(new { success = false, message = "Usuario no autenticado." }) { StatusCode = 401 };
            }

            var review = await _reviewService.GetReviewByIdAsync(request.ReviewId);
            if (review is null)
            {
                _logger.LogWarning("OnPostToggleTrackingAjaxAsync: Reseña con ID '{ReviewId}' no encontrada para tracking AJAX.", request.ReviewId);
                return new JsonResult(new { success = false, message = "Reseña no encontrada." }) { StatusCode = 404 };
            }

            // ✅ Validar que review.GameId no sea Guid.Empty
            if (review.GameId == Guid.Empty)
            {
                _logger.LogError("OnPostToggleTrackingAjaxAsync: Reseña con ID '{ReviewId}' tiene un GameId vacío.", request.ReviewId);
                return new JsonResult(new { success = false, message = "ID de juego asociado a la reseña inválido." }) { StatusCode = 400 };
            }

            var tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, review.GameId.ToString()) ?? new GameTrackingDto
            {
                UserId = userId,
                GameId = review.GameId.ToString()
            };
            _logger.LogDebug("OnPostToggleTrackingAjaxAsync: Tracking cargado para el juego '{GameId}' y usuario '{UserId}'. ID Tracking: {TrackingId}", review.GameId, userId, tracking.Id);

            switch (request.Type)
            {
                case "watch":
                    tracking.Status = tracking.Status == "played" ? null : "played";
                    break;
                case "watchlist":
                    tracking.Status = tracking.Status == "backlog" ? null : "backlog";
                    break;
                case "like":
                    tracking.Liked = !tracking.Liked;
                    break;
                default:
                    _logger.LogWarning("OnPostToggleTrackingAjaxAsync: Tipo de seguimiento '{TrackingType}' no reconocido para la reseña '{ReviewId}'.", request.Type, request.ReviewId);
                    return new JsonResult(new { success = false, message = "Tipo de seguimiento no reconocido." }) { StatusCode = 400 };
            }

            if (tracking.Id == Guid.Empty)
            {
                await _gameTrackingService.CreateAsync(tracking);
                _logger.LogInformation("OnPostToggleTrackingAjaxAsync: Nuevo seguimiento creado para el juego '{GameId}' por el usuario '{UserId}'.", review.GameId, userId);
            }
            else
            {
                await _gameTrackingService.UpdateAsync(tracking.Id, tracking);
                _logger.LogInformation("OnPostToggleTrackingAjaxAsync: Seguimiento actualizado para el juego '{GameId}' por el usuario '{UserId}'.", review.GameId, userId);
            }

            return new JsonResult(new
            {
                success = true,
                type = request.Type,
                isActive = request.Type switch
                {
                    "watch" => tracking.Status == "played",
                    "watchlist" => tracking.Status == "backlog",
                    "like" => tracking.Liked == true,
                    _ => false
                }
            });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostToggleTrackingAjaxAsync: HttpRequestException al actualizar el seguimiento para la reseña '{ReviewId}'. Mensaje: {Message}", request.ReviewId, ex.Message);
            return new JsonResult(new { success = false, message = $"Problema de conexión al actualizar el seguimiento: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostToggleTrackingAjaxAsync: Error inesperado al actualizar el seguimiento para la reseña '{ReviewId}'. Mensaje: {Message}", request.ReviewId, ex.Message);
            return new JsonResult(new { success = false, message = $"Error al actualizar el seguimiento: {ex.Message}" }) { StatusCode = 500 };
        }
    }


    public async Task<IActionResult> OnPostDeleteReviewAsync(string ReviewId)
    {
        // ✅ Validar ReviewId al inicio
        if (string.IsNullOrWhiteSpace(ReviewId))
        {
            _logger.LogWarning("OnPostDeleteReviewAsync: ID de reseña nulo o vacío para eliminar.");
            TempData["ErrorMessage"] = "ID de reseña no válido para eliminar.";
            return BadRequest();
        }

        try
        {
            // ✅ Validar roles antes de cualquier operación costosa
            if (!User.IsInRole("admin") && !User.IsInRole("moderator"))
            {
                _logger.LogWarning("OnPostDeleteReviewAsync: Usuario '{UserId}' sin permisos intentó eliminar la reseña '{ReviewId}'.", User.FindFirstValue(ClaimTypes.NameIdentifier), ReviewId);
                TempData["ErrorMessage"] = "No tienes permisos para eliminar esta reseña.";
                return Forbid();
            }

            var review = await _reviewService.GetReviewByIdAsync(ReviewId);
            if (review == null)
            {
                _logger.LogWarning("OnPostDeleteReviewAsync: Reseña con ID '{ReviewId}' no encontrada para eliminar.", ReviewId);
                TempData["ErrorMessage"] = "Reseña no encontrada para eliminar.";
                return NotFound();
            }
            // ✅ Validar que review.UserId no sea nulo o vacío antes de pasarlo al servicio
            if (string.IsNullOrWhiteSpace(review.UserId))
            {
                _logger.LogError("OnPostDeleteReviewAsync: La reseña '{ReviewId}' tiene un UserId nulo o vacío.", ReviewId);
                TempData["ErrorMessage"] = "El propietario de la reseña es inválido.";
                return BadRequest();
            }

            await _reviewService.DeleteReviewAsync(ReviewId, review.UserId);
            _logger.LogInformation("OnPostDeleteReviewAsync: Reseña '{ReviewId}' eliminada exitosamente por el usuario '{UserId}'.", ReviewId, User.FindFirstValue(ClaimTypes.NameIdentifier));
            SuccessMessage = "Reseña eliminada exitosamente.";
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostDeleteReviewAsync: HttpRequestException al eliminar la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al eliminar la reseña: {ex.Message}";
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostDeleteReviewAsync: Error inesperado al eliminar la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar la reseña: {ex.Message}";
        }
        return RedirectToPage("/Reviews/Reviews");
    }


    public async Task<IActionResult> OnPostReportReviewAsync(string ContentId, string ContentType, string Reason, DateTime CreatedAt)
    {
        // ✅ Validar parámetros de entrada al inicio
        if (string.IsNullOrWhiteSpace(ContentId) || string.IsNullOrWhiteSpace(ContentType) || string.IsNullOrWhiteSpace(Reason))
        {
            _logger.LogWarning("OnPostReportReviewAsync: Datos de reporte incompletos (ContentId: '{ContentId}', ContentType: '{ContentType}', Reason vacía).", ContentId, ContentType);
            TempData["ErrorMessage"] = "Datos de reporte incompletos.";
            return RedirectToPage("/Reviews/Details", new { reviewId = ContentId });
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("OnPostReportReviewAsync: Usuario no autenticado intentando reportar contenido '{ContentId}'.", ContentId);
                TempData["ErrorMessage"] = "Usuario no autenticado para reportar.";
                return Unauthorized();
            }

            var review = await _reviewService.GetReviewByIdAsync(ContentId);
            if (review == null)
            {
                _logger.LogWarning("OnPostReportReviewAsync: Reseña con ID '{ContentId}' no encontrada para reportar.", ContentId);
                TempData["ErrorMessage"] = "Reseña a reportar no encontrada.";
                return RedirectToPage("/Reviews/Details", new { reviewId = ContentId });
            }
            // ✅ Validar que review.UserId no sea nulo o vacío
            if (string.IsNullOrWhiteSpace(review.UserId))
            {
                _logger.LogError("OnPostReportReviewAsync: La reseña '{ContentId}' tiene un UserId nulo o vacío.", ContentId);
                TempData["ErrorMessage"] = "El propietario del contenido a reportar es inválido.";
                return RedirectToPage("/Reviews/Details", new { reviewId = ContentId });
            }

            var dto = new ReportDTO
            {
                Id = Guid.NewGuid().ToString(),
                ReportedUserId = review.UserId.Trim(),
                ContentType = ContentType,
                Reason = Reason,
                ReportedAt = CreatedAt,
                Status = "pending"
            };
            // ✅ Considerar validar que CreatedAt no sea DateTime.MinValue. Si es una fecha predeterminada, registrar advertencia.
            if (CreatedAt == DateTime.MinValue)
            {
                _logger.LogWarning("OnPostReportReviewAsync: CreatedAt es DateTime.MinValue para el reporte de la reseña '{ContentId}'.", ContentId);
            }

            await _moderationReportService.CreateAsync(dto);
            _logger.LogInformation("OnPostReportReviewAsync: Reseña '{ContentId}' reportada exitosamente por el usuario '{UserId}'. Tipo: {ContentType}, Razón: '{Reason}'", ContentId, userId, ContentType, Reason);
            SuccessMessage = "Reseña reportada exitosamente.";
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostReportReviewAsync: HttpRequestException al reportar la reseña '{ContentId}'. Mensaje: {Message}", ContentId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al reportar la reseña: {ex.Message}";
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostReportReviewAsync: Error inesperado al reportar la reseña '{ContentId}'. Mensaje: {Message}", ContentId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al reportar la reseña: {ex.Message}";
        }
        return RedirectToPage("/Reviews/Details", new { reviewId = ContentId });
    }


    public async Task<IActionResult> OnPostLogReviewWithTrackingAsync(Guid GameId, string Content, double Rating, DateTime? WatchedOn, bool PlayedBefore, bool Like)
    {
        // ✅ Validar GameId y Content al inicio
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

            // Crear y guardar la reseña
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

            // Obtener o crear tracking
            var tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, GameId.ToString());
            tracking ??= new GameTrackingDto
            {
                UserId = userId,
                GameId = GameId.ToString()
            };

            if (WatchedOn.HasValue || PlayedBefore)
            {
                tracking.Status = "played";
                tracking.Liked = Like;
            }
            // ✅ Asegurar que el ID de tracking sea correcto antes de actualizar/crear
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

            return new JsonResult(new { success = true, message = "Reseña y seguimiento guardados exitosamente." });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostLogReviewWithTrackingAsync: HttpRequestException al registrar reseña/tracking para el juego '{GameId}'. Mensaje: {Message}", GameId, ex.Message);
            return new JsonResult(new { success = false, message = $"Problema de conexión al registrar la reseña: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostLogReviewWithTrackingAsync: Error inesperado al registrar reseña/tracking para el juego '{GameId}'. Mensaje: {Message}", GameId, ex.Message);
            return new JsonResult(new { success = false, message = "Ocurrió un error en el servidor al intentar registrar la reseña." }) { StatusCode = 500 };
        }
    }


    [BindProperty]
    public string NewListName { get; set; } = ""; // ✅ Se usa directamente el BindProperty

    [BindProperty]
    public bool IsPublic { get; set; } // ✅ Se usa directamente el BindProperty

    public async Task<IActionResult> OnPostCreateListAsync(string NewListName, bool IsPublic, Guid GameId)
    {
        // ✅ Validar parámetros de entrada
        if (string.IsNullOrWhiteSpace(NewListName))
        {
            _logger.LogWarning("OnPostCreateListAsync: El nombre de la lista está vacío.");
            return new JsonResult(new { success = false, message = "El nombre de la lista no puede estar vacío." }) { StatusCode = 400 };
        }
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostCreateListAsync: ID de juego es Guid.Empty para crear la lista.");
            return new JsonResult(new { success = false, message = "ID de juego no válido para crear la lista." }) { StatusCode = 400 };
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostCreateListAsync: Usuario no autenticado intentando crear lista.");
                return new JsonResult(new { success = false, message = "Usuario no autenticado." }) { StatusCode = 401 };
            }

            var newList = new GameListDTO
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                Name = NewListName,
                IsPublic = IsPublic,
                Description = "",
                CreatedAt = DateTime.UtcNow
            };

            var createdListId = await _gameListService.CreateListAsync(newList);
            if (string.IsNullOrEmpty(createdListId))
            {
                _logger.LogError("OnPostCreateListAsync: El servicio CreateListAsync devolvió un ID nulo/vacío. Falló la creación de la lista '{NewListName}'.", NewListName);
                return new JsonResult(new { success = false, message = "Error al crear la lista." }) { StatusCode = 500 };
            }

            var item = new GameListItemDTO
            {
                Id = Guid.NewGuid().ToString(),
                GameId = GameId,
                ListId = createdListId
            };

            var added = await _gameListItemService.AddItemAsync(item);
            if (!added)
            {
                _logger.LogError("OnPostCreateListAsync: Falló al añadir el juego '{GameId}' a la lista recién creada '{CreatedListId}'. Eliminando la lista.", GameId, createdListId);
                // Considera eliminar la lista recién creada si el juego no se puede añadir.
                await _gameListService.DeleteListAsync(createdListId);
                return new JsonResult(new { success = false, message = "No se pudo agregar el juego a la nueva lista." }) { StatusCode = 500 };
            }

            _logger.LogInformation("OnPostCreateListAsync: Lista '{NewListName}' (ID: '{CreatedListId}') creada y juego '{GameId}' añadido exitosamente por el usuario '{UserId}'.", NewListName, createdListId, GameId, userId);
            return new JsonResult(new { success = true, message = "Lista creada y juego añadido exitosamente." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostCreateListAsync: HttpRequestException al crear lista '{NewListName}'. Mensaje: {Message}", NewListName, ex.Message);
            return new JsonResult(new { success = false, message = $"Problema de conexión al crear lista: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostCreateListAsync: Error inesperado al crear lista '{NewListName}'. Mensaje: {Message}", NewListName, ex.Message);
            return new JsonResult(new { success = false, message = $"Error al crear lista: {ex.Message}" }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostAddGameToListAsync(string ListId, Guid GameId)
    {
        // ✅ Validar parámetros de entrada
        if (string.IsNullOrWhiteSpace(ListId))
        {
            _logger.LogWarning("OnPostAddGameToListAsync: ID de lista nulo o vacío.");
            return new JsonResult(new { success = false, message = "ID de lista no válido." }) { StatusCode = 400 };
        }
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameToListAsync: ID de juego es Guid.Empty para añadir a la lista '{ListId}'.", ListId);
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
                _logger.LogInformation("OnPostAddGameToListAsync: Juego '{GameId}' ya existe en la lista '{ListId}'.", GameId, ListId);
                return new JsonResult(new { success = false, message = "Este juego ya está en la lista seleccionada." });
            }

            var dto = new GameListItemDTO
            {
                Id = Guid.NewGuid().ToString(),
                GameId = GameId,
                ListId = ListId
            };

            var success = await _gameListItemService.AddItemAsync(dto);

            if (!success)
            {
                _logger.LogError("OnPostAddGameToListAsync: Falló al añadir el juego '{GameId}' a la lista '{ListId}'. El servicio AddItemAsync devolvió false.", GameId, ListId);
                return new JsonResult(new { success = false, message = "No se pudo agregar el juego a la lista." }) { StatusCode = 500 };
            }

            _logger.LogInformation("OnPostAddGameToListAsync: Juego '{GameId}' añadido exitosamente a la lista '{ListId}' por el usuario '{UserId}'.", GameId, ListId, userId);
            return new JsonResult(new { success = true, message = "Juego agregado a la lista exitosamente." });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostAddGameToListAsync: HttpRequestException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", GameId, ListId, ex.Message);
            return new JsonResult(new { success = false, message = $"Problema de conexión al agregar el juego: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostAddGameToListAsync: Error inesperado al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", GameId, ListId, ex.Message);
            return new JsonResult(new { success = false, message = $"Error al agregar juego a la lista: {ex.Message}" }) { StatusCode = 500 };
        }
    }

    public async Task<IActionResult> OnPostToggleLikeAsync(string ReviewId)
    {
        // ✅ Validar ReviewId al inicio
        if (string.IsNullOrWhiteSpace(ReviewId))
        {
            _logger.LogWarning("OnPostToggleLikeAsync: ID de reseña nulo o vacío.");
            return new JsonResult(new { success = false, message = "ID de reseña no válido para dar like." }) { StatusCode = 400 };
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostToggleLikeAsync: Usuario no autenticado intentando dar like/unlike a la reseña '{ReviewId}'.", ReviewId);
                return new JsonResult(new { success = false, message = "Usuario no autenticado para dar like." }) { StatusCode = 401 };
            }

            var reviewExists = await _reviewService.GetReviewByIdAsync(ReviewId);
            if (reviewExists == null)
            {
                _logger.LogWarning("OnPostToggleLikeAsync: Reseña con ID '{ReviewId}' no encontrada para dar like/unlike.", ReviewId);
                return new JsonResult(new { success = false, message = "Reseña no encontrada." }) { StatusCode = 404 };
            }

            var userLiked = await _reviewService.HasUserLikedAsync(ReviewId, userId);

            bool nowLiked;

            if (userLiked)
            {
                await _reviewService.UnlikeReviewAsync(ReviewId, userId);
                nowLiked = false;
                _logger.LogInformation("OnPostToggleLikeAsync: Usuario '{UserId}' quitó el like a la reseña '{ReviewId}'.", userId, ReviewId);
            }
            else
            {
                await _reviewService.LikeReviewAsync(ReviewId, userId);
                nowLiked = true;
                _logger.LogInformation("OnPostToggleLikeAsync: Usuario '{UserId}' dio like a la reseña '{ReviewId}'.", userId, ReviewId);
            }

            return new JsonResult(new { liked = nowLiked, success = true });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnPostToggleLikeAsync: HttpRequestException al alternar like para la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            return new JsonResult(new { success = false, message = $"Problema de conexión al alternar like: {ex.Message}" }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnPostToggleLikeAsync: Error inesperado al alternar like para la reseña '{ReviewId}'. Mensaje: {Message}", ReviewId, ex.Message);
            return new JsonResult(new { success = false, message = $"Error al alternar like: {ex.Message}" }) { StatusCode = 500 };
        }
    }

    // Clase de modelo para ReportReviewInputModel si no está definida en otro lugar
    public class ReportReviewInputModel
    {
        public string ContentId { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CommentViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string UserAvatarUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TrackingRequest
    {
        public string ReviewId { get; set; } = "";
        public string Type { get; set; } = "";
    }
}