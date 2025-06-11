using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

[Authorize]
public class CreatorModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userService;

    public CreatorModel(IGameService gameService, IReviewService reviewService, IAuthService authService, IUserManagerService userManager)
    {
        _gameService = gameService;
        _reviewService = reviewService;
        _authService = authService;
        _userService = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; }

    [BindProperty]
    public ReviewDTO Review { get; set; } = new();

    public GamePreviewDTO? Game { get; set; }

    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (GameId == Guid.Empty)
            return NotFound("Game ID is required");

        Game = await _gameService.GetGamePreviewByIdAsync(GameId);
        Review.GameId = GameId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Game = await _gameService.GetGamePreviewByIdAsync(GameId);

        var ratingStr = Request.Form["RatingHack"];
        if (double.TryParse(ratingStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRating))
        {
            Review.Rating = parsedRating;
        }
        else
        {
            ModelState.AddModelError("Review.Rating", "Invalid rating value.");
        }

        Console.WriteLine("📥 RatingHack: " + ratingStr);
        Console.WriteLine("📥 Review.Rating: " + Review.Rating);
        Console.WriteLine($"📥 Review.Rating: {Review.Rating}");


        //if (!ModelState.IsValid)
         //   return Page();

        Review.GameId = GameId;
        Review.CreatedAt = DateTime.UtcNow;
        Review.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo";
        Review.Likes = 0;
        Review.LikedBy = new List<string>();

        await _reviewService.CreateReviewAsync(Review);

        SuccessMessage = "Review submitted successfully!";
        return Page();
    }
}
