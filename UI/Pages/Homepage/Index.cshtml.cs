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

    public List<ReviewFullDto> RecentReviewCards { get; set; } = new List<ReviewFullDto>();
    public List<ReviewFullDto> PopularReviewCards { get; set; } = new List<ReviewFullDto>();
    public List<ReviewFullDto> ReviewCards { get; set; } = new List<ReviewFullDto>();
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
        UserId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value; 

        ViewData["WelcomeMessage"] = $"Welcome back, {User.Identity?.Name ?? "User"}. Here’s what we’ve been watching…";

        var popularReviewsData = await _reviewService.GetTopReviewsAsync(10);
        await LoadReviewCards(popularReviewsData, PopularReviewCards);

        var recentReviewsData = await _reviewService.GetRecentReviewsAsync(10);
        await LoadReviewCards(recentReviewsData, RecentReviewCards);

        
        var reviewsForImagesSource = await _reviewService.GetRecentReviewsAsync(10);
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

        var gamePreviewsSource = await _gameService.GetGamePreviewsAsync();
        Games = gamePreviewsSource?.Take(24).ToList() ?? new List<GamePreviewDTO>();


        var listsDataSource = await _gameListService.GetRecentListsAsync();


        if (listsDataSource != null)
        {
            var publicLists = listsDataSource.Where(list => list.IsPublic).ToList();
            foreach (var list in publicLists)
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
    
    // Dentro de IndexModel.cs, en el método LoadReviewCards
private async Task LoadReviewCards(List<ReviewDTO>? reviews, List<ReviewFullDto> targetList) // Nota: 'targetList' es List<ReviewFullDto>
{
    if (reviews != null)
    {
        foreach (var review in reviews)
        {
            // Llama a IUserManagerService para obtener el perfil del usuario
            var userProfileTask = _userService.GetProfileAsync(review.UserId);
            // Llama a IGameService para obtener la vista previa del juego
            var gamePreviewTask = _gameService.GetGamePreviewByIdAsync(review.GameId);
            // Llama a IAuthService para obtener el nombre de usuario (authUser)
            var authUserTask = _authService.SearchUserByIdAsync(review.UserId);

            await Task.WhenAll(userProfileTask, gamePreviewTask, authUserTask); // Ejecuta en paralelo

            var user = userProfileTask.Result;
            var game = gamePreviewTask.Result;
            var authUser = authUserTask.Result;

            var gameImageUrl = game?.HeaderUrl ?? "/Images/noImage.png";
            var profileImageUrl = user?.AvatarUrl ?? "/Images/noImage.png";
            var userNameDisplay = authUser?.Username ?? "Usuario desconocido";
            var gameTitleDisplay = game?.Title ?? "Juego desconocido"; // <--- AQUÍ SE OBTIENE EL TÍTULO DEL JUEGO

            var userLiked = review.LikedBy != null && review.LikedBy.Contains(UserId);

            targetList.Add(new ReviewFullDto // <--- AQUÍ SE CREA EL ReviewFullDto
            {
                Id = review.Id,
                Content = review.Content,
                Likes = review.Likes,
                Rating = review.Rating,
                GameId = review.GameId.ToString(),
                UserName = userNameDisplay,
                ProfileImageUrl = profileImageUrl,
                GameImageUrl = gameImageUrl,
                CreatedAt = review.CreatedAt,
                UserLiked = userLiked,
                GameTitle = gameTitleDisplay // <--- Y AQUÍ SE ASIGNA EL TÍTULO AL DTO COMPLETO
            });
        }
    }
}
    
}
