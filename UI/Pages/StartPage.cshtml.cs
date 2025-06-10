
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class StartPageModel : PageModel
{
    private readonly ILogger<StartPageModel> _logger;
    private readonly IAuthService _authService;
    private readonly IGameService _gameService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;


    private readonly IUserManagerService _userService;

    private readonly IReviewService _reviewService;
    [BindProperty]
    public AuthRequestDto LoginData { get; set; } = new();

    [BindProperty]
    public RegisterRequestDto RegisterData { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public List<GamePreviewDTO> Games { get; set; }
    public List<ReviewDTO> Reviews { get; set; }

    public List<ReviewWithUserDto> ReviewCards {get; set;}
    public List<GameListWithUserDto> RecentLists { get; set; }

    public List<ImageReviewDto> ReviewImages { get; set; }
    public StartPageModel(ILogger<StartPageModel> logger, IAuthService authService, IGameService gameService, IReviewService reviewService, IUserManagerService userService, IGameListItemService gameListItemService, IGameListService gameListService) 
    {
        _logger = logger;
        _authService = authService;
        _gameService = gameService;
        _reviewService = reviewService;
        _userService = userService;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
    }

   public async Task<IActionResult> OnPostLoginAsync()
{
    var result = await _authService.LoginAsync(LoginData);

    if (result != null)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,               
            Secure = false,                
            SameSite = SameSiteMode.Strict, 
            Expires = DateTime.UtcNow.AddDays(7) 
        };

        Response.Cookies.Append("DuskSkyToken", result.AccessToken, cookieOptions);

       

        return RedirectToPage("/Homepage/Index"); 
    }

    ErrorMessage = "Email or password incorrect.";
    return Page();
}


    public async Task<IActionResult> OnPostRegisterAsync()
{
    var registerResult = await _authService.RegisterAsync(RegisterData);
    if (registerResult != null)
    {
        var loginRequest = new AuthRequestDto
        {
            Username = RegisterData.Username,
            Password = RegisterData.Password
        };

        var loginResult = await _authService.LoginAsync(loginRequest);
        if (loginResult != null && !string.IsNullOrWhiteSpace(loginResult.AccessToken))
        {
            Response.Cookies.Append("DuskSkyToken", loginResult.AccessToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return RedirectToPage("/Homepage/Index");
        }

        ErrorMessage = "Registro exitoso, pero fallo al iniciar sesión.";
        return Page();
    }

    ErrorMessage = "Registro fallido.";
    return Page();
}

    public async Task OnGetAsync()
    {
        var previews = await _gameService.GetGamePreviewsAsync();
        foreach (var preview in previews)
        {
        }
        Games = previews.Take(24).ToList();

        var recentReviews = await _reviewService.GetRecentReviewsAsync();
        var top10 = recentReviews.Take(10).ToList();

        ReviewImages = new();

        var seen = new HashSet<Guid>(); // o string si tu ID es string

        foreach (var review in top10)
        {
            if (!seen.Add(review.GameId)) // Add devuelve false si ya existía
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

        var recentReviewsCards = await _reviewService.GetRecentReviewsAsync(6);
        ReviewCards = new List<ReviewWithUserDto>();


        foreach (var review in recentReviewsCards)
        {
            var user = await _userService.GetProfileAsync(review.UserId); // 👈 aquí llamas al método
            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
            ReviewCards.Add(new ReviewWithUserDto
            {
                Content = review.Content,
                Likes = review.Likes,
                Rating = review.Rating,
                GameId = review.GameId,
                UserName = user?.Username ?? "Usuario desconocido",
                ProfileImageUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                GameImageUrl = game.HeaderUrl ?? "/Images/noImage.png"
            });
        }

        int minCards = 10;

        while (ReviewImages.Count < minCards)
        {
            ReviewImages.Add(new ImageReviewDto
            {
                HeaderUrl = "/Images/noImage.png"
            });

        }

        RecentLists = new List<GameListWithUserDto>();

var lists = await _gameListService.GetRecentListsAsync();

foreach (var list in lists)
{
    var userTask = _userService.GetProfileAsync(list.UserId);
    var itemsTask = _gameListItemService.GetItemsByListIdAsync(list.Id);

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
        UserName = user?.Username ?? "Usuario desconocido",
        AvatarUrl = user?.AvatarUrl ?? "/Images/noImage.png",
        GameHeaders = headersUrl
    });
}



    

        



        
    }

    
}