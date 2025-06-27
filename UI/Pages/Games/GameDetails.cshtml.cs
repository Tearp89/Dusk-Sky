using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class GameDetailsModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IReviewService _reviewService;
    private readonly IUserManagerService _userService;
    private readonly IAuthService _authService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;

    public GameDetailsModel(
        IGameService gameService,
        IReviewService reviewService,
        IUserManagerService userService,
        IAuthService authService,
        IGameTrackingService gameTrackingService,
        IGameListService gameListService,
        IGameListItemService gameListItemService)
    {
        _gameService = gameService;
        _reviewService = reviewService;
        _userService = userService;
        _authService = authService;
        _gameTrackingService = gameTrackingService;
        _gameListService = gameListService;
        _gameListItemService = gameListItemService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid gameId { get; set; }

    public GameDetailsDTO? Game { get; set; } = default!;
    public GamePreviewDTO GamePreview { get; set; }
    public List<ReviewFullDto> Reviews { get; set; } = new();
    public GameTrackingDto? Tracking { get; set; }
    public List<GameListDTO> UserLists { get; set; } = new();


    public bool IsWatched => Tracking?.Status == "played";
    public bool IsInWatchlist => Tracking?.Status == "backlog";
    public bool IsLiked => Tracking?.Liked == true;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Cargar GameDetailsDTO completo (ya tiene HeaderUrl y RandomScreenshot)
        Game = await _gameService.GetGameByIdAsync(gameId);
        if (Game == null) return NotFound();

        // 2. Configurar el fondo borroso
        // Usa un screenshot aleatorio. Si no hay, usa el HeaderUrl. Si tampoco, usa un default.
        var backgroundImageUrl = !string.IsNullOrEmpty(Game.RandomScreenshot)
                                 ? Game.RandomScreenshot
                                 : (string.IsNullOrEmpty(Game.HeaderUrl) ? "/Images/default-game-background.png" : Game.HeaderUrl);
        
        ViewData["BackgroundImage"] = Url.Content(backgroundImageUrl);
        ViewData["UseBlurEffect"] = true; // Asegura que el blur está activo para el fondo

        // 3. Cargar las listas del usuario
        UserLists = await _gameListService.GetUserListsAsync(userId);

        // 4. Cargar el estado de seguimiento del juego para el usuario actual
        Tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, gameId.ToString());

        // 5. Cargar y enriquecer las reseñas del juego
        var allReviews = await _reviewService.GetTopReviewsByGameAsync(gameId.ToString());
        var reviewTasks = new List<Task<ReviewFullDto>>(); // Para ejecutar en paralelo

        if (allReviews != null)
        {
            foreach (var review in allReviews)
            {
                reviewTasks.Add(EnrichReviewFullDtoAsync(review, Game.Title, Game.HeaderUrl, userId));
            }
            Reviews = (await Task.WhenAll(reviewTasks)).OrderByDescending(r => r.CreatedAt).ToList();
        }
        
        return Page();
    }

    // Método auxiliar para enriquecer ReviewFullDto (similar a LoadReviewCards)
    private async Task<ReviewFullDto> EnrichReviewFullDtoAsync(ReviewDTO review, string gameTitle, string gameImageUrl, string? currentUserId)
    {
        var profileTask = _userService.GetProfileAsync(review.UserId);
        var accountTask = _authService.SearchUserByIdAsync(review.UserId);

        await Task.WhenAll(profileTask, accountTask);

        var profile = profileTask.Result;
        var account = accountTask.Result;

        return new ReviewFullDto
        {
            Id = review.Id,
            GameId = review.GameId.ToString(), 
            GameTitle = gameTitle, 
            GameImageUrl = gameImageUrl, 
            UserId = review.UserId,
            UserName = account?.Username ?? "Unknown",
            ProfileImageUrl = profile?.AvatarUrl ?? "/Images/noImage.png",
            Content = review.Content,
            Rating = review.Rating,
            CreatedAt = review.CreatedAt,
            Likes = review.Likes,
            LikedBy = review.LikedBy,
            UserLiked = currentUserId != null && review.LikedBy.Contains(currentUserId)
        };
    }

    public async Task<IActionResult> OnPostToggleTrackingAsync(string trackingType)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToPage();

        Tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, gameId.ToString()) ?? new GameTrackingDto
        {
            UserId = userId,
            GameId = gameId.ToString()
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
        }

        if (Tracking.Id == Guid.Empty)

            await _gameTrackingService.CreateAsync(Tracking);
        else
            await _gameTrackingService.UpdateAsync(Tracking.Id, Tracking);

        return RedirectToPage(new { gameId });
    }

    public async Task<IActionResult> OnPostAddGameToListAsync(string ListId, Guid GameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false, message = "Usuario no autenticado." });

        var alreadyExists = await _gameListItemService.ExistsAsync(ListId.ToString(), GameId);
        if (alreadyExists)
            return new JsonResult(new { success = false, message = "Game already in the list." });

        var dto = new GameListItemDTO
        {
            Id = Guid.NewGuid().ToString(),
            GameId = GameId,
            ListId = ListId
        };

        var success = await _gameListItemService.AddItemAsync(dto);

        return new JsonResult(new { success });
    }

    public async Task<IActionResult> OnPostLogReviewWithTrackingAsync(Guid GameId, string Content, double Rating, DateTime? WatchedOn, bool PlayedBefore, bool Like)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return new JsonResult(new { success = false, message = "Usuario no autenticado." });

    var review = new ReviewDTO
    {
        GameId = GameId,
        UserId = userId,
        Content = Content,
        Rating = Rating,
        CreatedAt = DateTime.UtcNow
    };

    await _reviewService.CreateReviewAsync(review);

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

        await _gameTrackingService.CreateAsync(tracking);
    else
        await _gameTrackingService.UpdateAsync(tracking.Id, tracking);

    return new JsonResult(new { success = true });
}




}
