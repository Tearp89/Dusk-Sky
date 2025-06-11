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
    private readonly IGameTrackingService _trackingService;
    private readonly IUserManagerService _userService;

    public CreatorModel(
        IGameService gameService,
        IReviewService reviewService,
        IGameTrackingService trackingService,
        IUserManagerService userManager)
    {
        _gameService = gameService;
        _reviewService = reviewService;
        _trackingService = trackingService;
        _userService = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; }

    [BindProperty]
    public ReviewDTO Review { get; set; } = new();

    [BindProperty]
    public GameTrackingDto Tracking { get; set; } = new();

    [BindProperty]
    public bool Watched { get; set; }

    [BindProperty]
    public bool WatchedBefore { get; set; }

    [BindProperty]
    public DateTime? WatchedOn { get; set; }

    public GamePreviewDTO? Game { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (GameId == Guid.Empty)
            return NotFound("Game ID is required");

        Game = await _gameService.GetGamePreviewByIdAsync(GameId);
        Review.GameId = GameId;
        Tracking.GameId = GameId.ToString();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Game = await _gameService.GetGamePreviewByIdAsync(GameId);

        var ratingStr = Request.Form["Review.Rating"];
        if (double.TryParse(ratingStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRating))
        {
            Review.Rating = parsedRating;
        }
        else
        {
            ModelState.AddModelError("Review.Rating", "Invalid rating value.");
        }

        if (!ModelState.IsValid)
            return Page();

        // Obtener ID del usuario actual
        string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "demo";

        // Rellenar campos de la review
        Review.GameId = GameId;
        Review.CreatedAt = DateTime.UtcNow;
        Review.UserId = userId;
        Review.Likes = 0;
        Review.LikedBy = new List<string>();

        await _reviewService.CreateReviewAsync(Review);

        // Rellenar campos del tracking
        Tracking.GameId = GameId.ToString();
        Tracking.UserId = userId;
        Tracking.Liked = Tracking.Liked;

        // Determinar estado
        if (Watched || WatchedBefore)
        {
            Tracking.Status = "played";
        }
        else
        {
            Tracking.Status = "backlog"; // o puedes dejarlo vacío si lo prefieres
        }

        await _trackingService.CreateAsync(Tracking);

        SuccessMessage = "Review and tracking saved successfully!";
        return Page();
    }
}
