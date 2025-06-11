using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UI.Pages;


[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IGameService _gameService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;
    private readonly IReviewService _reviewService;
    private readonly IUserManagerService _userService;
    public List<ReviewWithUserDto> ReviewCards { get; set; }
    public List<ImageReviewDto> ReviewImages { get; set; }
    public List<GameListWithUserDto> RecentLists { get; set; }
    public List<GamePreviewDTO> Games { get; set; }
    private readonly IAuthService _authService;
    
    [BindProperty]
    public string UserId { get; set; }

    public IndexModel(ILogger<IndexModel> logger, IGameService gameService, IGameListItemService gameListItemService, IGameListService gameListService, IReviewService reviewService, IUserManagerService userManagerService, IAuthService authService)
    {
        _logger = logger;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
        _gameService = gameService;
        _reviewService = reviewService;
        _userService = userManagerService;
        _authService = authService;
    }

public async Task<IActionResult> OnPostToggleLikeAsync(string ReviewId)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

    return new JsonResult(new { liked = nowLiked });
}

    

    public async Task<IActionResult> OnGetAsync()
    {
            var UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var avatar = User.FindFirst("avatar_url")?.Value;

        // Puedes guardar esto en el ViewData o en un DTO y pasarlo al Razor


        var recentReviewsCards = await _reviewService.GetRecentReviewsAsync(6);
        ReviewCards = new List<ReviewWithUserDto>();


        foreach (var review in recentReviewsCards)
        {
            var user = await _userService.GetProfileAsync(review.UserId); // 👈 aquí llamas al método
            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
            var userWithNameReview = await _authService.SearchUserByIdAsync(review.UserId);
            ReviewCards.Add(new ReviewWithUserDto
            {
                Id = review.Id,
                Content = review.Content,
                Likes = review.Likes,
                Rating = review.Rating,
                GameId = review.GameId,
                UserName = userWithNameReview?.Username ?? "Usuario desconocido",
                ProfileImageUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                GameImageUrl = game.HeaderUrl ?? "/Images/noImage.png",
                CreatedAt = review.CreatedAt,
                UserLiked = review.LikedBy.Contains(UserId)
            });
        }

        int minCards = 10;

        ReviewImages = new List<ImageReviewDto>();
        while (ReviewImages.Count < minCards)
        {
            ReviewImages.Add(new ImageReviewDto
            {
                HeaderUrl = "/Images/noImage.png"
            });

        }

        var previews = await _gameService.GetGamePreviewsAsync();
        foreach (var preview in previews)
        {
        }
        Games = previews.Take(24).ToList();

        var recentReviews = await _reviewService.GetRecentReviewsAsync();
        var top10 = recentReviews.Take(10).ToList();

        ReviewImages = new();

        var seen = new HashSet<Guid>();

        foreach (var review in top10)
        {
            if (!seen.Add(review.GameId))
                continue;

            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
            if (game != null)
            {
                ReviewImages.Add(new ImageReviewDto
                {
                    HeaderUrl = game.HeaderUrl
                });
            }
        }

        RecentLists = new List<GameListWithUserDto>();

        var lists = await _gameListService.GetRecentListsAsync();

        foreach (var list in lists)
        {
            var userTask = _userService.GetProfileAsync(list.UserId);
            var itemsTask = _gameListItemService.GetItemsByListIdAsync(list.Id);
            var userWithName = await _authService.SearchUserByIdAsync(list.UserId);
            await Task.WhenAll(userTask, itemsTask);

            var user = userTask.Result;
            var items = itemsTask.Result;

            var headersUrl = new List<string>();

            // ✅ Agregamos varias imágenes (máximo 4 para no saturar visualmente)
            foreach (var item in items.Take(4))
            {
                var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                if (!string.IsNullOrEmpty(game?.HeaderUrl))
                    headersUrl.Add(game.HeaderUrl);
                else
                    headersUrl.Add("/Images/noImage.png");
            }

            // En caso de que no haya imágenes (lista vacía), agregamos una por defecto
            if (headersUrl.Count == 0)
                headersUrl.Add("/Images/noImage.png");

            RecentLists.Add(new GameListWithUserDto
            {
                Id = list.Id,
                Name = list.Name,
                Description = list.Description,
                IsPublic = list.IsPublic,
                UserId = list.UserId,
                Date = list.CreatedAt,
                UserName = userWithName?.Username ?? "Usuario desconocido",
                AvatarUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                GameHeaders = headersUrl
            });
        }

       


       
        return Page();
    }
    
}
