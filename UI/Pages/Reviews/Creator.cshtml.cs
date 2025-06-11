using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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

    [TempData]
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
        // Re-cargar datos del juego para el formulario
        Game = await _gameService.GetGamePreviewByIdAsync(GameId);

        // Forzar cultura a Invariant para parsing correcto
        var ratingStr = Request.Form["Review.Rating"];
        if (double.TryParse(ratingStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedRating))
        {
            Review.Rating = parsedRating;
        }
        else
        {
            ModelState.AddModelError("Review.Rating", "Rating must be a number between 0.5 and 5.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        Review.GameId = GameId;
        Review.CreatedAt = DateTime.UtcNow;
        Review.UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo";
        Review.Likes = 0;
        Review.LikedBy = new List<string>();

        await _reviewService.CreateReviewAsync(Review);

        SuccessMessage = "Review submitted successfully!";
        return RedirectToPage("/Reviews/Creator", new { gameId = GameId });
    }
}
