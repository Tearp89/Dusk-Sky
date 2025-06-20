using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Asegúrate de que los using apunten a tus servicios y ViewModels
// using YourApp.Services;
// using YourApp.ViewModels;

// Asumo que tu ReviewDto que viene del servicio se ve algo así:



public class ReviewsProfileModel : ProfileModelBase // Hereda de la clase base
{
    // --- Servicios necesarios para esta página y para la clase base ---
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IGameListService _listService;
    private readonly IGameService _gameService;
    private readonly IGameTrackingService _gameTrackingService;

    // --- Propiedad para guardar las reseñas de este usuario ---
    public List<ReviewCardViewModel> UserReviews { get; set; } = new();

    public ReviewsProfileModel(
        IReviewService reviewService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IGameListService listService,
        IGameService gameService,
        IGameTrackingService gameTrackingService)
    {
        _reviewService = reviewService;
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _listService = listService;
        _gameService = gameService;
        _gameTrackingService = gameTrackingService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Reviews"; 

        var userExists = await LoadProfileHeaderData(
            userId, 
            _authService, 
            _userManagerService, 
            _friendshipService,
            _reviewService,
            _listService,
            _gameTrackingService);
            
        if (!userExists)
        {
            return NotFound();
        }

        var reviewDocs = await _reviewService.GetFriendsReviewsAsync(new List<string> { userId });

        if (reviewDocs != null && reviewDocs.Any())
        {
            var reviewTasks = reviewDocs.Select(dto => MapToViewModelAsync(dto));
            UserReviews = (await Task.WhenAll(reviewTasks)).ToList();
        }

        return Page();
    }

    private async Task<ReviewCardViewModel> MapToViewModelAsync(ReviewDTO reviewDto)
    {
        var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);
        
        return new ReviewCardViewModel
        {
            ReviewId = reviewDto.Id,
            GameId = reviewDto.GameId.ToString(),
            GameTitle = game?.Title ?? "Unknown Game",
            GameImageUrl = game?.HeaderUrl ?? "/images/noImage.png",
            UserId = reviewDto.UserId,
            UserName = this.ProfileHeader.Username, 
            UserAvatarUrl = this.ProfileHeader.AvatarUrl, 
            Content = reviewDto.Content,
            Rating = reviewDto.Rating,
            LikesCount = reviewDto.Likes,
            CreatedAt = reviewDto.CreatedAt
        };
    }
}