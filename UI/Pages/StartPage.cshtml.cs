
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

public class StartPageModel : PageModel
{
    private readonly ILogger<StartPageModel> _logger;
    private readonly IAuthService _authService;
    private readonly IGameService _gameService;
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService;
    private readonly UserSessionManager _sessionManager;

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
    
    public List<UserSearchResultDto> Users { get; set; }
    public StartPageModel(ILogger<StartPageModel> logger, IAuthService authService, IGameService gameService, IReviewService reviewService, IUserManagerService userService, IGameListItemService gameListItemService, IGameListService gameListService, UserSessionManager userSessionManager)
    {
        _logger = logger;
        _authService = authService;
        _gameService = gameService;
        _reviewService = reviewService;
        _userService = userService;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
        _sessionManager = userSessionManager;
    }

   public async Task<IActionResult> OnPostLoginAsync()
{
    var result = await _authService.LoginAsync(LoginData);

    if (result != null && !string.IsNullOrWhiteSpace(result.AccessToken))
    {
        var token = result.AccessToken;

        // Buscar usuarios por nombre exacto
        var users = await _authService.SearchUsersAsync(LoginData.Username);
        var user = users.FirstOrDefault(u => u.Username == LoginData.Username);

        if (user == null)
        {
            ErrorMessage = "No se encontró el usuario con ese nombre.";
            return Page();
        }

        var profile = await _userService.GetProfileAsync(user.Id);
        if (profile == null)
        {
            ErrorMessage = "No se pudo obtener el perfil del usuario.";
            return Page();
        }

        UserSessionManager.Instance.SetSession(token, user.Id.ToString(), user.Username);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("avatar_url", profile.AvatarUrl ?? "/images/default_avatar.png")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTime.UtcNow.AddDays(7)
            });

        Response.Cookies.Append("DuskSkyToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(7)
        });

        return RedirectToPage("/Homepage/Index");
    }

    ErrorMessage = "Email o contraseña incorrectos.";
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
            var token = loginResult.AccessToken;

            // Buscar usuario
            var users = await _authService.SearchUsersAsync(RegisterData.Username);
            var user = users.FirstOrDefault(u => u.Username == RegisterData.Username);

            if (user == null)
            {
                ErrorMessage = "Registro exitoso, pero no se encontró el usuario.";
                return Page();
            }

            var profile = await _userService.GetProfileAsync(user.Id);
            if (profile == null)
            {
                ErrorMessage = "Registro exitoso, pero no se pudo obtener el perfil del usuario.";
                return Page();
            }

            UserSessionManager.Instance.SetSession(token, user.Id.ToString(), user.Username);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("avatar_url", profile.AvatarUrl ?? "/images/default_avatar.png")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(7)
                });

            Response.Cookies.Append("DuskSkyToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
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


    public async Task<IActionResult> OnGetAsync()
    {
        var previews = await _gameService.GetGamePreviewsAsync();
        foreach (var preview in previews)
        {
        }
        Games = previews.Take(24).ToList();

        var recentReviews = await _reviewService.GetRecentReviewsAsync();
        var top10 = recentReviews.Take(10).ToList();

        ReviewImages = new();

        var seen = new HashSet<Guid>(); 

        foreach (var review in top10)
        {
            if (!seen.Add(review.GameId)) 
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
            var userWithName = await _authService.SearchUserByIdAsync(review.UserId);
            
            ReviewCards.Add(new ReviewWithUserDto
            {
                Id = review.Id,
                Content = review.Content,
                Likes = review.Likes,
                Rating = review.Rating,
                GameId = review.GameId,
                UserName = userWithName?.Username ?? "Usuario desconocido",
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
            var userWithName = await _authService.SearchUserByIdAsync(list.UserId);

            await Task.WhenAll(userTask, itemsTask);

            var user = userTask.Result;
            var items = itemsTask.Result;
            var userWithNameList = await _authService.SearchUserByIdAsync(list.UserId);

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
                UserName = userWithNameList?.Username ?? "Usuario desconocido",
                AvatarUrl = user?.AvatarUrl ?? "/Images/noImage.png",
                GameHeaders = headersUrl
            });
        }


        return Page();










    }

    
}