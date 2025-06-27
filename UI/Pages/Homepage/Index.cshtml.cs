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
    
    private readonly IAuthService _authService;

    public List<ReviewWithUserDto> ReviewCards { get; set; } = new List<ReviewWithUserDto>();
    public List<ImageReviewDto> ReviewImages { get; set; } = new List<ImageReviewDto>();
    public List<GameListWithUserDto> RecentLists { get; set; } = new List<GameListWithUserDto>();
    public List<GamePreviewDTO> Games { get; set; } = new List<GamePreviewDTO>();
    
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
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
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
        // Dado que la página es [Authorize], sabemos que el usuario está autenticado.
        // Obtén el ID del usuario actual.
        UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value; // Usar ! ya que [Authorize] garantiza que no es null.

        // Puedes usar el Username del usuario autenticado directamente.
        ViewData["WelcomeMessage"] = $"Welcome back, {User.Identity?.Name ?? "User"}. Here’s what we’ve been watching…";


        // Carga de reseñas recientes para las tarjetas
        var recentReviewsCardsData = await _reviewService.GetRecentReviewsAsync(20);
        
        if (recentReviewsCardsData != null)
        {
            foreach (var review in recentReviewsCardsData)
            {
                var userProfileTask = _userService.GetProfileAsync(review.UserId);
                var gamePreviewTask = _gameService.GetGamePreviewByIdAsync(review.GameId);
                var authUserTask = _authService.SearchUserByIdAsync(review.UserId);

                await Task.WhenAll(userProfileTask, gamePreviewTask, authUserTask);

                var user = userProfileTask.Result;
                var game = gamePreviewTask.Result;
                var authUser = authUserTask.Result;

                var gameImageUrl = game?.HeaderUrl ?? "/Images/noImage.png";
                var profileImageUrl = user?.AvatarUrl ?? "/Images/noImage.png";
                var userNameDisplay = authUser?.Username ?? "Usuario desconocido";
                
                // Asumimos que review.LikedBy no es null si el servicio lo devuelve así,
                // o que tu ReviewDto lo inicializa como new List<string>().
                var userLiked = review.LikedBy != null && review.LikedBy.Contains(UserId);

                ReviewCards.Add(new ReviewWithUserDto
                {
                    Id = review.Id,
                    Content = review.Content,
                    Likes = review.Likes,
                    Rating = review.Rating,
                    GameId = review.GameId,
                    UserName = userNameDisplay,
                    ProfileImageUrl = profileImageUrl,
                    GameImageUrl = gameImageUrl,
                    CreatedAt = review.CreatedAt,
                    UserLiked = userLiked
                });
            }
        }

        // Carga de ReviewImages
        var reviewsForImagesSource = await _reviewService.GetRecentReviewsAsync();
        var topReviewsForImages = reviewsForImagesSource?.Take(10).ToList() ?? new List<ReviewDTO>();

        ReviewImages = new List<ImageReviewDto>();
        var seenGameIdsForImages = new HashSet<Guid>();

        foreach (var review in topReviewsForImages)
        {
            if (!seenGameIdsForImages.Add(review.GameId))
                continue;

            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
            if (game != null && !string.IsNullOrEmpty(game.HeaderUrl))
            {
                ReviewImages.Add(new ImageReviewDto { HeaderUrl = game.HeaderUrl });
            }
            else
            {
                ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
            }
        }
        int minReviewImagesCount = 6;
        while (ReviewImages.Count < minReviewImagesCount)
        {
            ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
        }

        // Carga de Games
        var gamePreviewsSource = await _gameService.GetGamePreviewsAsync();
        Games = gamePreviewsSource?.Take(24).ToList() ?? new List<GamePreviewDTO>();


        // Carga de listas recientes
        var listsDataSource = await _gameListService.GetRecentListsAsync();

        if (listsDataSource != null)
        {
            foreach (var list in listsDataSource)
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
                        if (!string.IsNullOrEmpty(game?.HeaderUrl))
                            headersUrl.Add(game.HeaderUrl);
                        else
                            headersUrl.Add("/Images/noImage.png");
                    }
                }
                
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
        }

        return Page();
    }
    
}
