using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

[Authorize]
public class ReviewDetailsModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly ICommentService _commentService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IGameTrackingService _gameTrackingService;
    

    public ReviewDetailsModel(
        IReviewService reviewService,
        ICommentService commentService,
        IUserManagerService userService,
        IGameService gameService,
        IAuthService authService,
        IGameTrackingService gameTrackingService)
    {
        _reviewService = reviewService;
        _commentService = commentService;
        _userService = userService;
        _gameService = gameService;
        _authService = authService;
        _gameTrackingService = gameTrackingService;
    }

    [BindProperty(SupportsGet = true)]
    public string ReviewId { get; set; } = string.Empty;
 

    public ReviewWithUserDto? Review { get; set; }
    public List<CommentViewModel> Comments { get; set; } = new();
    public GameTrackingDto Tracking { get; set; } = new();
    public GamePreviewDTO GamePreview { get; set; } 
    
    public bool IsWatched => Tracking?.Status == "played"; 
    public bool IsInWatchlist => Tracking?.Status == "backlog";
    public bool IsLiked => Tracking?.Liked == true;
    public string UserId { get; set; }
    public async Task<IActionResult> OnGetAsync()
    {
        var UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;





        if (string.IsNullOrWhiteSpace(ReviewId))
            return NotFound();

        var reviewDto = await _reviewService.GetReviewByIdAsync(ReviewId);
        if (reviewDto is null)
            return NotFound();

        // Cargar datos del usuario autor de la review
        var userProfile = await _userService.GetProfileAsync(reviewDto.UserId);
        var userAccount = await _authService.SearchUserByIdAsync(reviewDto.UserId);

        // Cargar datos del juego asociado
        var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);

        GamePreview = new GamePreviewDTO
        {
            Id = game.Id,
            Title = game.Title,
            HeaderUrl = game.HeaderUrl
        };

        Review = new ReviewWithUserDto
        {
            Id = reviewDto.Id,
            GameId = reviewDto.GameId,
            UserId = reviewDto.UserId,
            Content = reviewDto.Content,
            Likes = reviewDto.Likes,
            CreatedAt = reviewDto.CreatedAt,
            Rating = reviewDto.Rating,
            LikedBy = reviewDto.LikedBy,
            UserLiked = UserId != null && reviewDto.LikedBy.Contains(UserId),
            UserName = userAccount?.Username ?? "Usuario desconocido",
            ProfileImageUrl = userProfile?.AvatarUrl ?? "/images/noavatar.png",
            GameImageUrl = game?.HeaderUrl ?? "/images/noimage.png"
        };

        Tracking = await _gameTrackingService.GetByUserAndGameAsync(UserId, Review.GameId.ToString());
        Tracking ??= new GameTrackingDto();




        // Obtener comentarios asociados a la review
        var commentList = await _commentService.GetCommentsByReviewIdAsync(ReviewId);
        foreach (var comment in commentList)
        {
            var author = await _userService.GetProfileAsync(comment.AuthorId);
            var authorUser = await _authService.SearchUserByIdAsync(comment.AuthorId);
            Comments.Add(new CommentViewModel
            {
                UserName = authorUser?.Username ?? "Anónimo",
                UserAvatarUrl = author?.AvatarUrl ?? "/images/noavatar.png",
                Content = comment.Text,
                CreatedAt = comment.CreatedAt
            });
        }

        return Page();
    }



public async Task<IActionResult> OnPostAgregarComentarioAsync(string reviewId, string nuevoComentario)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    Console.WriteLine("Comentario recibido: " + nuevoComentario);

    if (string.IsNullOrWhiteSpace(nuevoComentario) || string.IsNullOrWhiteSpace(userId))
        return RedirectToPage("/Reviews/Details", new { reviewId });

    var nuevo = new CommentDTO
    {
        ReviewId = reviewId,
        AuthorId = userId,
        Text = nuevoComentario,
        CreatedAt = DateTime.UtcNow,
        Status = CommentStatus.Visible
    };

    await _commentService.CreateCommentAsync(nuevo);

    return RedirectToPage("/Reviews/Details", new { reviewId });
}


    public class CommentViewModel
    {
        public string UserName { get; set; } = "";
        public string UserAvatarUrl { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
