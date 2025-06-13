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
    private readonly IGameListItemService _gameListItemService;
    private readonly IGameListService _gameListService;


    public ReviewDetailsModel(
        IReviewService reviewService,
        ICommentService commentService,
        IUserManagerService userService,
        IGameService gameService,
        IAuthService authService,
        IGameTrackingService gameTrackingService,
        IGameListItemService gameListItemService,
        IGameListService gameListService)
    {
        _reviewService = reviewService;
        _commentService = commentService;
        _userService = userService;
        _gameService = gameService;
        _authService = authService;
        _gameTrackingService = gameTrackingService;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
    }

    [BindProperty(SupportsGet = true)]
    public string ReviewId { get; set; } = string.Empty;
    [TempData]
    public string? SuccessMessage { get; set; }


    public ReviewWithUserDto? Review { get; set; }
    public List<CommentViewModel> Comments { get; set; } = new();
    public GameTrackingDto Tracking { get; set; } = new();
    public GamePreviewDTO GamePreview { get; set; } = new();
    public ReviewDTO ReviewDTO { get; set; } = new();
    public List<ReviewWithGameDto> UserReviews { get; set; } = new();
    public List<GameListDTO> UserLists { get; set; } = new();



    public bool IsWatched => Tracking?.Status == "played";
    public bool IsInWatchlist => Tracking?.Status == "backlog";
    public bool IsLiked => Tracking?.Liked == true;
    public string UserId { get; set; }
    public List<string> UserIds { get; set; } = new();


    public async Task<IActionResult> OnGetAsync()
    {
        UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


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

        UserReviews = new List<ReviewWithGameDto>();
        UserIds.Add(UserId);

        var userReviews = await _reviewService.GetFriendsReviewsAsync(UserIds);

        foreach (var review in userReviews)
        {
            var gameReview = await _gameService.GetGamePreviewByIdAsync(review.GameId);

            if (gameReview != null)
            {
                UserReviews.Add(new ReviewWithGameDto
                {
                    ReviewId = review.Id, 
                    GameId = review.GameId.ToString(),
                    GameTitle = gameReview.Title,
                    GameImageUrl = gameReview.HeaderUrl
                });
            }
        }
        UserLists = await _gameListService.GetUserListsAsync(UserId);



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

    public async Task<IActionResult> OnPostToggleTrackingAsync(string reviewId, string trackingType)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Reviews/Details", new { reviewId });

        var review = await _reviewService.GetReviewByIdAsync(reviewId);
        if (review is null)
            return RedirectToPage("/Reviews/Details", new { reviewId });

        var currentTracking = await _gameTrackingService.GetByUserAndGameAsync(userId, review.GameId.ToString());
        currentTracking ??= new GameTrackingDto
        {
            UserId = userId,
            GameId = review.GameId.ToString(),
            Liked = false,
            Status = null
        };

        switch (trackingType)
        {
            case "watch":
                currentTracking.Status = currentTracking.Status == "played" ? null : "played";
                break;

            case "watchlist":
                currentTracking.Status = currentTracking.Status == "backlog" ? null : "backlog";
                break;

            case "like":
                currentTracking.Liked = !currentTracking.Liked;
                break;
        }

        await _gameTrackingService.UpdateAsync(currentTracking.Id, currentTracking);

        return RedirectToPage("/Reviews/Details", new { reviewId });
    }

    public async Task<IActionResult> OnPostToggleTrackingAjaxAsync([FromBody] TrackingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return new JsonResult(new { success = false });

        var review = await _reviewService.GetReviewByIdAsync(request.ReviewId);
        if (review is null)
            return new JsonResult(new { success = false });

        var tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, review.GameId.ToString()) ?? new GameTrackingDto
        {
            UserId = userId,
            GameId = review.GameId.ToString()
        };

        switch (request.Type)
        {
            case "watch":
                tracking.Status = tracking.Status == "played" ? null : "played";
                break;
            case "watchlist":
                tracking.Status = tracking.Status == "backlog" ? null : "backlog";
                break;
            case "like":
                tracking.Liked = !tracking.Liked;
                break;
        }

        await _gameTrackingService.UpdateAsync(tracking.Id, tracking);

        return new JsonResult(new
        {
            success = true,
            type = request.Type,
            isActive = request.Type switch
            {
                "watch" => tracking.Status == "played",
                "watchlist" => tracking.Status == "backlog",
                "like" => tracking.Liked == true,
                _ => false
            }
        });
    }

    public async Task<IActionResult> OnPostLogReviewWithTrackingAsync(Guid GameId, string Content, double Rating, DateTime? WatchedOn, bool PlayedBefore, bool Like)
    {
        Console.WriteLine("Entró al handler LogReviewWithTracking");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return RedirectToPage("/StartPage");
        Console.WriteLine("REVIEW CONTENT: " + Content);

        var review = new ReviewDTO
        {
            GameId = GameId,
            UserId = userId,
            Content = Content,
            Rating = Rating,
            CreatedAt = DateTime.UtcNow
        };
        await _reviewService.CreateReviewAsync(review);

        var tracking = await _gameTrackingService.GetByUserAndGameAsync(userId, GameId.ToString());

        if (tracking == null)
        {
            tracking = new GameTrackingDto
            {
                UserId = userId,
                GameId = GameId.ToString()
            };
        }

        if (WatchedOn.HasValue || PlayedBefore)
        {
            tracking.Status = "played";
            tracking.Liked = Like;
        }

        if (string.IsNullOrEmpty(tracking.Id.ToString()))
            await _gameTrackingService.CreateAsync(tracking);
        else
            await _gameTrackingService.UpdateAsync(tracking.Id, tracking);

        return new JsonResult(new { success = true });
    }
    
    [BindProperty]
public string NewListName { get; set; } = "";

[BindProperty]
public bool IsPublic { get; set; }

public async Task<IActionResult> OnPostCreateListAsync(string NewListName, bool IsPublic, Guid GameId)
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
        return new JsonResult(new { success = false, message = "User not found." });

    var newList = new GameListDTO
    {
        Id = Guid.NewGuid().ToString(),
        UserId = userId,
        Name = NewListName,
        IsPublic = IsPublic,
        Description = "",
        CreatedAt = DateTime.UtcNow
    };

    var created = await _gameListService.CreateListAsync(newList);
    if (!created)
        return new JsonResult(new { success = false, message = "Failed to create list." });

    var userLists = await _gameListService.GetUserListsAsync(userId);
    var list = userLists.FirstOrDefault(l => l.Name == NewListName);

    if (list == null)
        return new JsonResult(new { success = false, message = "List not found after creation." });

    var item = new GameListItemDTO
    {
        GameId = GameId,
        ListId = list.Id
    };

    var added = await _gameListItemService.AddItemAsync(item);
    if (!added)
        return new JsonResult(new { success = false, message = "Failed to add game to list." });

    return new JsonResult(new { success = true });
}




    

public class CreateListRequest
    {
        public string Name { get; set; } = "";
        public bool IsPrivate { get; set; }
        public string GameId { get; set; } = "";
    }







    public class TrackingRequest
    {
        public string ReviewId { get; set; } = "";
        public string Type { get; set; } = "";
    }




    public class CommentViewModel
    {
        public string UserName { get; set; } = "";
        public string UserAvatarUrl { get; set; } = "";
        public string Content { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
