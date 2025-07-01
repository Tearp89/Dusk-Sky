using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UI.Pages;

[Authorize]
public class IndexModel : PageModel
{
    // --- Servicios y Propiedades (Sin cambios) ---
    private readonly ILogger<IndexModel> _logger;
    private readonly IGameService _gameService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;
    private readonly IReviewService _reviewService;
    private readonly IUserManagerService _userService;
    private readonly IFriendshipService _friendshipService;
    private readonly IAuthService _authService;

    public List<ReviewFullDto> RecentReviewCards { get; set; } = new();
    public List<ReviewFullDto> PopularReviewCards { get; set; } = new();
    public List<ReviewFullDto> ReviewCards { get; set; } = new();
    public List<ImageReviewDto> ReviewImages { get; set; } = new();
    public List<GameListWithUserDto> RecentLists { get; set; } = new();
    public List<GamePreviewDTO> Games { get; set; } = new();
    public bool HasFriends { get; set; }
    private string? currentUserId; // Guardar el ID del usuario para usarlo en los helpers
    public string UserId { get; private set; }
    private readonly SemaphoreSlim _throttle = new(10); // Máximo 10 tareas al mismo tiempo


    // MANTENEMOS: Constructor con validaciones de nulos
    public IndexModel(ILogger<IndexModel> logger, IGameService gameService, IGameListItemService gameListItemService, IGameListService gameListService, IReviewService reviewService, IUserManagerService userManagerService, IAuthService authService, IFriendshipService friendshipService)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(gameService);
        ArgumentNullException.ThrowIfNull(gameListItemService);
        ArgumentNullException.ThrowIfNull(gameListService);
        ArgumentNullException.ThrowIfNull(reviewService);
        ArgumentNullException.ThrowIfNull(userManagerService);
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(friendshipService);

        _logger = logger;
        _gameService = gameService;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
        _reviewService = reviewService;
        _userService = userManagerService;
        _authService = authService;
        _friendshipService = friendshipService;
    }

    // MANTENEMOS: OnPostToggleLikeAsync con manejo de errores
    public async Task<IActionResult> OnPostToggleLikeAsync(string ReviewId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(ReviewId))
        {
            return new JsonResult(new { success = false, message = "Datos inválidos." }) { StatusCode = 400 };
        }
        try
        {
            // Asumo que no tienes un ToggleLikeAsync, así que mantengo la lógica original
            var userLiked = await _reviewService.HasUserLikedAsync(ReviewId, userId);
            bool nowLiked;
            if (userLiked)
            {
                await _reviewService.UnlikeReviewAsync(ReviewId, userId);
                nowLiked = false;
            }
            else
            {
                await _reviewService.LikeReviewAsync(ReviewId, userId);
                nowLiked = true;
            }
            return new JsonResult(new { success = true, liked = nowLiked });
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error en OnPostToggleLikeAsync para usuario {UserId} y reseña {ReviewId}", userId, ReviewId);
            return new JsonResult(new { success = false, message = "No se pudo procesar la solicitud." }) { StatusCode = 500 };
        }
    }


    // MODIFICADO: OnGetAsync con un try-catch general
    public async Task<IActionResult> OnGetAsync()
    {
        currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId)) return Challenge();
        this.UserId = currentUserId;

        try
        {
            ViewData["WelcomeMessage"] = $"Welcome back, {User.Identity?.Name ?? "User"}. Here’s what we’ve been playing...";

            // Lógica para determinar amigos (ya era segura, la mantenemos)
            var friendIds = (await _friendshipService.GetFriendsAsync(currentUserId) ?? new())
                .Select(f => f.SenderId == currentUserId ? f.ReceiverId : f.SenderId).ToList();
            HasFriends = friendIds.Any();

            // Carga de datos secuencial pero segura
            if (HasFriends)
            {
                var friendsReviews = await _reviewService.GetFriendsReviewsAsync(friendIds, 6);
                await LoadReviewCards(friendsReviews, ReviewCards);
            }
            else
            {
                var recentReviewsForDuskSky = await _reviewService.GetRecentReviewsAsync(6);
                await LoadReviewCards(recentReviewsForDuskSky, ReviewCards);
            }

            var popularReviewsData = await _reviewService.GetTopReviewsAsync(10);
            await LoadReviewCards(popularReviewsData, PopularReviewCards);

            var recentReviewsData = await _reviewService.GetRecentReviewsAsync(10);
            await LoadReviewCards(recentReviewsData, RecentReviewCards);

            await LoadReviewImages();
            await LoadGames();
            await LoadRecentLists();

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error catastrófico al cargar Index para el usuario {UserId}", currentUserId);
            TempData["ErrorMessage"] = "Ocurrió un error al cargar la página. Por favor, intenta de nuevo.";
            return Page();
        }
    }
    
    // --- MÉTODOS DE AYUDA MODIFICADOS PARA SER RESILIENTES ---

    // MODIFICADO: LoadReviewCards con try-catch interno por cada reseña
    private async Task LoadReviewCards(List<ReviewDTO>? reviews, List<ReviewFullDto> targetList)
    {
        if (reviews == null) return;

        foreach (var review in reviews)
        {
            try // NUEVO: Si falla una reseña, no se detiene el proceso
            {
                var userProfileTask = _userService.GetProfileAsync(review.UserId);
                var gamePreviewTask = _gameService.GetGamePreviewByIdAsync(review.GameId);
                var authUserTask = _authService.SearchUserByIdAsync(review.UserId);

                await Task.WhenAll(userProfileTask, gamePreviewTask, authUserTask);

                var user = userProfileTask.Result;
                var game = gamePreviewTask.Result;
                var authUser = authUserTask.Result;

                targetList.Add(new ReviewFullDto
                {
                    Id = review.Id,
                    Content = review.Content,
                    Likes = review.Likes,
                    Rating = review.Rating,
                    GameId = review.GameId.ToString(),
                    UserName = authUser?.Username ?? "Usuario desconocido",
                    ProfileImageUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                    GameImageUrl = game?.HeaderUrl ?? "/Images/noImage.png",
                    GameTitle = game?.Title ?? "Juego desconocido",
                    CreatedAt = review.CreatedAt,
                    UserLiked = review.LikedBy?.Contains(currentUserId!) ?? false
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar la tarjeta para la reseña ID {ReviewId}. Se omitirá.", review.Id);
            }
        }
    }

    // Los siguientes métodos ya tenían una estructura bastante segura,
    // pero los encapsulamos para mayor claridad en OnGetAsync.

    private async Task LoadReviewImages()
    {
        var reviewsForImagesSource = await _reviewService.GetRecentReviewsAsync(10);
        var topReviewsForImages = reviewsForImagesSource?.Take(10).ToList() ?? new List<ReviewDTO>();
        var seenGameIdsForImages = new HashSet<Guid>();

        foreach (var review in topReviewsForImages)
        {
            try
            {
                if (!seenGameIdsForImages.Add(review.GameId)) continue;
                
                var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
                ReviewImages.Add(new ImageReviewDto { HeaderUrl = game?.HeaderUrl ?? "/Images/noImage.png" });
            }
            catch (Exception ex)
            {
                 _logger.LogWarning(ex, "No se pudo cargar la imagen para el juego con ID de reseña {ReviewId}. Se usará imagen por defecto.", review.Id);
                 ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
            }
        }
        while (ReviewImages.Count < 6)
        {
            ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
        }
    }

    private async Task LoadGames()
    {
        var gamePreviewsSource = await _gameService.GetGamePreviewsAsync();
        Games = gamePreviewsSource?.Take(24).ToList() ?? new List<GamePreviewDTO>();
    }

    private async Task LoadRecentLists()
    {
        var listsDataSource = (await _gameListService.GetRecentListsAsync())?.Where(list => list.IsPublic).ToList() ?? new();
        
        foreach (var list in listsDataSource)
        {
            try // NUEVO: Si falla una lista, no se detiene el proceso
            {
                var userTask = _userService.GetProfileAsync(list.UserId);
                var itemsTask = _gameListItemService.GetItemsByListIdAsync(list.Id);
                var userWithNameTask = _authService.SearchUserByIdAsync(list.UserId);
                await Task.WhenAll(userTask, itemsTask, userWithNameTask);

                var user = userTask.Result;
                var items = itemsTask.Result;
                var userWithName = userWithNameTask.Result;

                var headersUrl = new List<string>();
                if (items != null)
                {
                    foreach (var item in items.Take(4))
                    {
                        var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                        headersUrl.Add(game?.HeaderUrl ?? "/Images/noImage.png");
                    }
                }
                if (!headersUrl.Any()) headersUrl.Add("/Images/noImage.png");

                RecentLists.Add(new GameListWithUserDto
                {
                    // ... (resto de las propiedades de la lista)
                    Id = list.Id, Name = list.Name, Description = list.Description, IsPublic = list.IsPublic, UserId = list.UserId,
                    Date = list.CreatedAt, UserName = userWithName?.Username ?? "Usuario desconocido", AvatarUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                    GameHeaders = headersUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo cargar la lista con ID {ListId}. Se omitirá.", list.Id);
            }
        }
    }
}