using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



public class ReviewsModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;

    public List<ReviewCardViewModel> PopularReviews { get; set; }
    public List<ReviewCardViewModel> RecentReviews { get; set; }

    public ReviewsModel(
        IReviewService reviewService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService)
    {
        _reviewService = reviewService;
        _gameService = gameService;
        _authService = authService;
        _userManagerService = userManagerService;
    }

    public async Task OnGetAsync()
    {
        var popularReviewDtos = await _reviewService.GetTopReviewsAsync();
        var recentReviewDtos = await _reviewService.GetRecentReviewsAsync();

        var popularTasks = popularReviewDtos.Select(dto => MapToViewModelAsync(dto));
        var recentTasks = recentReviewDtos.Select(dto => MapToViewModelAsync(dto));

        PopularReviews = (await Task.WhenAll(popularTasks)).ToList();
        RecentReviews = (await Task.WhenAll(recentTasks)).ToList();
    }

    private async Task<ReviewCardViewModel> MapToViewModelAsync(ReviewDTO reviewDto)
    {
        var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);
        var authUser = await _authService.SearchUserByIdAsync(reviewDto.UserId); // Asumo que tienes un método así
        var profile = await _userManagerService.GetProfileAsync(reviewDto.UserId);

        return new ReviewCardViewModel
        {
            ReviewId = reviewDto.Id,
            GameId = reviewDto.GameId.ToString(),
            GameTitle = game?.Title ?? "Unknown Game",
            GameImageUrl = game?.HeaderUrl ?? "/images/noImage.png",
            UserId = reviewDto.UserId,
            UserName = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
            UserAvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png",
            Content = reviewDto.Content,
            Rating = reviewDto.Rating,
            LikesCount = reviewDto.Likes,
            CreatedAt = reviewDto.CreatedAt
        };
    }
}

