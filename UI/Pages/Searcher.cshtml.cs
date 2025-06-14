using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SearcherModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _listService;

    public SearcherModel(
        IGameService gameService,
        IAuthService authService,
        IReviewService reviewService,
        IGameListService listService)
    {
        _gameService = gameService;
        _authService = authService;
        _reviewService = reviewService;
        _listService = listService;
    }

    [BindProperty(SupportsGet = true)]
    public string Query { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public List<GamePreviewDTO> Games { get; set; } = new();
    public List<UserSearchResultDto> Users { get; set; } = new();
    public List<ReviewWithUserDto> Reviews { get; set; } = new();
    public List<GameListDTO> Lists { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;

        if (Filter is null or "all")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
            Users = await _authService.SearchUsersAsync(Query);

            // Fallback mientras no existe búsqueda real para reseñas/listas
            var recentReviews = await _reviewService.GetRecentReviewsAsync();
            Reviews = recentReviews
                .Where(r => r.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .Select(r => new ReviewWithUserDto
                {
                    Id = r.Id,
                    GameId = r.GameId,
                    UserId = r.UserId,
                    Content = r.Content,
                    Likes = r.Likes,
                    CreatedAt = r.CreatedAt,
                    Rating = r.Rating,
                    UserName = "Unknown",
                    ProfileImageUrl = "/Images/noImage.png",
                    GameImageUrl = "/Images/noImage.png",
                    LikedBy = r.LikedBy,
                    UserLiked = false
                })
                .ToList();

            var recentLists = await _listService.GetRecentListsAsync();
            Lists = recentLists
    .Where(l =>
        (!string.IsNullOrEmpty(l.Name) && l.Name.Contains(Query, StringComparison.OrdinalIgnoreCase)) ||
        (!string.IsNullOrEmpty(l.Description) && l.Description.Contains(Query, StringComparison.OrdinalIgnoreCase)))
    .ToList();

        }
        else if (Filter == "games")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
        }
        else if (Filter == "users")
        {
            Users = await _authService.SearchUsersAsync(Query);
        }
        else if (Filter == "reviews")
        {
            var recentReviews = await _reviewService.GetRecentReviewsAsync();
            Reviews = recentReviews
                .Where(r => r.Content.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .Select(r => new ReviewWithUserDto
                {
                    Id = r.Id,
                    GameId = r.GameId,
                    UserId = r.UserId,
                    Content = r.Content,
                    Likes = r.Likes,
                    CreatedAt = r.CreatedAt,
                    Rating = r.Rating,
                    UserName = "Unknown",
                    ProfileImageUrl = "/Images/noImage.png",
                    GameImageUrl = "/Images/noImage.png",
                    LikedBy = r.LikedBy,
                    UserLiked = false
                })
                .ToList();
        }
        else if (Filter == "lists")
        {
            var recentLists = await _listService.GetRecentListsAsync();
            Lists = recentLists
                .Where(l => l.Name.Contains(Query, StringComparison.OrdinalIgnoreCase) ||
                            l.Description.Contains(Query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
