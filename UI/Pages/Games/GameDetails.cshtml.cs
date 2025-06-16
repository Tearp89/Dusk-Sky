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

    public GameDetailsDTO? Game { get; set; }
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

        Game = await _gameService.GetGameByIdAsync(gameId);
        GamePreview = await _gameService.GetGamePreviewByIdAsync(gameId);
        if (Game == null) return NotFound();

        UserLists = await _gameListService.GetUserListsAsync(userId);


        Tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, gameId.ToString());

        var allReviews = await _reviewService.GetReviewsByGameAsync(gameId.ToString());
        var reviewDtos = new List<ReviewFullDto>();

        foreach (var review in allReviews)
        {
            var profile = await _userService.GetProfileAsync(review.UserId);
            var account = await _authService.SearchUserByIdAsync(review.UserId);

            reviewDtos.Add(new ReviewFullDto
            {
                Id = review.Id,
                GameId = review.GameId.ToString(),
                GameTitle = Game.Title,
                GameImageUrl = "", // opcional
                UserId = review.UserId,
                UserName = account?.Username ?? "Unknown",
                ProfileImageUrl = profile?.AvatarUrl ?? "/Images/noImage.png",
                Content = review.Content,
                Rating = review.Rating,
                CreatedAt = review.CreatedAt,
                Likes = review.Likes,
                LikedBy = review.LikedBy,
                UserLiked = userId != null && review.LikedBy.Contains(userId)
            });
        }

        Reviews = reviewDtos.OrderByDescending(r => r.CreatedAt).ToList();

        return Page();
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

        if (string.IsNullOrEmpty(Tracking.Id.ToString()))
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


    
}
