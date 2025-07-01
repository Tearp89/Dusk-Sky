using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http; 
using System.Security.Claims;
using System.Text.Json; 
using System.Threading.Tasks;



[AllowAnonymous]
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
    private readonly IModerationSanctionService _sanctionService;

    [BindProperty]
    public AuthRequestDto LoginData { get; set; } = new();

    [BindProperty]
    public RegisterRequestDto RegisterData { get; set; } = new();

    
    public string? ErrorMessage { get; set; } 

    public List<GamePreviewDTO> Games { get; set; } = new(); 
    public List<ReviewDTO> Reviews { get; set; } = new(); 
    public List<ReviewFullDto> ReviewCards { get; set; } = new(); 
    public List<GameListWithUserDto> RecentLists { get; set; } = new(); 
    public List<ImageReviewDto> ReviewImages { get; set; } = new(); 
    public List<UserSearchResultDto> Users { get; set; } = new(); 

    public StartPageModel(
        ILogger<StartPageModel> logger,
        IAuthService authService,
        IGameService gameService,
        IReviewService reviewService,
        IUserManagerService userService,
        IGameListItemService gameListItemService,
        IGameListService gameListService,
        UserSessionManager userSessionManager,
        IModerationSanctionService sanctionService)
    {
        _logger = logger;
        _authService = authService;
        _gameService = gameService;
        _reviewService = reviewService;
        _userService = userService;
        _gameListItemService = gameListItemService;
        _gameListService = gameListService;
        _sessionManager = userSessionManager; 
        _sanctionService = sanctionService;
    }

    public async Task<IActionResult> OnPostLoginAsync()
    {
        _logger.LogInformation("Attempting login for user: {Username}", LoginData.Username);

        
        

        AuthResponseDto? result = null;
        try
        {
            result = await _authService.LoginAsync(LoginData);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error during login attempt for user: {Username}", LoginData.Username);
            ErrorMessage = "Connection error: Could not connect to the authentication service. Please try again later.";
            return Page();
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error during login for user: {Username}", LoginData.Username);
            ErrorMessage = "Error processing server response. Please try again later.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during login attempt for user: {Username}", LoginData.Username);
            ErrorMessage = "An unexpected error occurred during login. Please try again.";
            return Page();
        }

        if (result != null && !string.IsNullOrWhiteSpace(result.AccessToken))
        {
            var token = result.AccessToken;
            UserSearchResultDto? user = null;
            try
            {
                var usersFound = await _authService.SearchUsersAsync(LoginData.Username);
                user = usersFound?.FirstOrDefault(u => u.Username == LoginData.Username);
                if (user.status == "deleted")
                {
                    return Page();
                }
                if (user == null)
                {
                    _logger.LogWarning("User '{Username}' not found after successful login. Possible data inconsistency.", LoginData.Username);
                    ErrorMessage = "User not found with that name after authentication.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for user '{Username}' after login.", LoginData.Username);
                ErrorMessage = "Error verifying user information. Please try again.";
                return Page();
            }

            try
            {
                var sanctions = await _sanctionService.GetAllAsync();
                if (sanctions == null)
                {
                    _logger.LogWarning("GetAllAsync for sanctions returned null. Initializing as empty list.");
                    sanctions = new List<SanctionDTO>(); 
                }

                _logger.LogInformation("Verifying sanctions for user: {UserId}", user.Id);
                foreach (var s in sanctions)
                {
                    _logger.LogDebug("Sanction for: {SanctionUserId} | Type: {SanctionType} | Dates: {StartDate} - {EndDate}",
                        s.UserId, s.Type, s.StartDate.ToString(CultureInfo.InvariantCulture), s.EndDate.ToString());
                }

                var hasActiveSanction = sanctions.Any(s =>
                    s.UserId.Trim() == user.Id.Trim() &&
                    (
                        s.Type == SanctionType.ban ||
                        (s.Type == SanctionType.suspension &&
                         s.StartDate <= DateTime.UtcNow &&
                         s.EndDate >= DateTime.UtcNow)
                    )
                );

                if (hasActiveSanction)
                {
                    _logger.LogWarning("Access denied for user {UserId} due to active sanction (ban/suspension).", user.Id);
                    ErrorMessage = "Accesso denegado: tu cuenta está baneada o suspendida.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying sanctions for user {UserId}.", user.Id);
                ErrorMessage = "Ocurrió un error verificando el estado de tu cuenta. Por favor intenta de nuevo.";
                return Page();
            }

            UserProfileDTO? profile = null;
            try
            {
                profile = await _userService.GetProfileAsync(user.Id);
                // Null validation for profile
                if (profile == null)
                {
                    _logger.LogWarning("User profile not found for ID: {UserId}. Using default avatar.", user.Id);
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user ID: {UserId}.", user.Id);
                ErrorMessage = "Error fetching your profile information. Please try again.";
                return Page();
            }

           
            try
            {
                _sessionManager.SetSession(token, user.Id.ToString(), user.Username);
                _logger.LogInformation("Custom session set for user: {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting custom session for user: {UserId}", user.Id);
                
            }

            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("avatar_url", profile?.AvatarUrl ?? "/images/default_avatar.png"),
                    new Claim(ClaimTypes.Role, user.Role ?? "user") 
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTime.UtcNow.AddDays(30)
                    });
                _logger.LogInformation("User {UserId} successfully authenticated into ASP.NET Core Cookies.", user.Id);

                Response.Cookies.Append("DuskSkyToken", token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = HttpContext.Request.IsHttps, 
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30),
                    
                });
                _logger.LogInformation("Cookie 'DuskSkyToken' set for user {UserId}.", user.Id);

                return RedirectToPage("/Homepage/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating claims or signing in to HttpContext for user {UserId}.", user.Id);
                ErrorMessage = "Error finalizing login. Please try again.";
                return Page();
            }
        }

        _logger.LogWarning("Login failed for user '{Username}'. Incorrect credentials or invalid service response.", LoginData.Username);
        ErrorMessage = "Incorrect email or password.";
        return Page();
    }


    public async Task<IActionResult> OnPostRegisterAsync()
    {
        _logger.LogInformation("Attempting registration for user: {Username}", RegisterData.Username);


        AuthResponseDto? registerResult = null;
        try
        {
            registerResult = await _authService.RegisterAsync(RegisterData);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Network error during user registration: {Username}", RegisterData.Username);
            ErrorMessage = "Connection error: Could not connect to the registration service. Please try again later.";
            return Page();
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "JSON deserialization error during user registration: {Username}", RegisterData.Username);
            ErrorMessage = "Error processing server response during registration.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during user registration: {Username}", RegisterData.Username);
            ErrorMessage = "An unexpected error occurred during registration. Please try again.";
            return Page();
        }


        if (registerResult != null)
        {
            _logger.LogInformation("Registration successful for user: {Username}. Attempting automatic login.", RegisterData.Username);
            var loginRequest = new AuthRequestDto
            {
                Username = RegisterData.Username,
                Password = RegisterData.Password
            };

            AuthResponseDto? loginResult = null;
            try
            {
                loginResult = await _authService.LoginAsync(loginRequest);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error attempting automatic login after registration for user: {Username}", RegisterData.Username);
                ErrorMessage = "Registration successful, but there was a connection error during automatic login. Please try to log in manually.";
                return Page();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "JSON deserialization error during automatic login after registration for user: {Username}", RegisterData.Username);
                ErrorMessage = "Registration successful, but error processing automatic login response. Please try to log in manually.";
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred attempting automatic login after registration for user: {Username}", RegisterData.Username);
                ErrorMessage = "Registration successful, but an unexpected error occurred during automatic login. Please try to log in manually.";
                return Page();
            }


            if (loginResult != null && !string.IsNullOrWhiteSpace(loginResult.AccessToken))
            {
                var token = loginResult.AccessToken;
                UserSearchResultDto? user = null;
                try
                {
                    var usersFound = await _authService.SearchUsersAsync(RegisterData.Username);
                    user = usersFound?.FirstOrDefault(u => u.Username == RegisterData.Username);
                    if (user == null)
                    {
                        _logger.LogWarning("User '{Username}' not found after successful registration/login. Data inconsistency.", RegisterData.Username);
                        ErrorMessage = "Registration successful, but complete user information was not found. Please try to log in manually.";
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error searching for user '{Username}' after registration/login.", RegisterData.Username);
                    ErrorMessage = "Registration successful, but error verifying user information. Please try to log in manually.";
                    return Page();
                }

                UserProfileDTO? profile = null;
                try
                {
                    profile = await _userService.GetProfileAsync(user.Id);
                    if (profile == null)
                    {
                        _logger.LogWarning("User profile not found for ID: {UserId} after registration. Using default avatar.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching profile for user ID: {UserId} after registration.", user.Id);
                    ErrorMessage = "Registration successful, but error fetching your profile information. Please try to log in manually.";
                    return Page();
                }

                
                try
                {
                    _sessionManager.SetSession(token, user.Id.ToString(), user.Username);
                    _logger.LogInformation("Custom session set for registered user: {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error setting custom session for registered user: {UserId}", user.Id);
                }

                try
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim("avatar_url", profile?.AvatarUrl ?? "/images/default_avatar.png"),
                        new Claim(ClaimTypes.Role, user.Role ?? "user")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        principal,
                        new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTime.UtcNow.AddDays(30)
                        });
                    _logger.LogInformation("Registered user {UserId} successfully authenticated into ASP.NET Core Cookies.", user.Id);

                    Response.Cookies.Append("DuskSkyToken", token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = HttpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTime.UtcNow.AddDays(30),
                        Domain = HttpContext.Request.Host.Host
                    });
                    _logger.LogInformation("Cookie 'DuskSkyToken' set for registered user {UserId}.", user.Id);

                    return RedirectToPage("/Homepage/Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating claims or signing in to HttpContext for registered user {UserId}.", user.Id);
                    ErrorMessage = "Registration successful, but there was an error finalizing login. Please try to log in manually.";
                    return Page();
                }
            }

            _logger.LogWarning("Registration successful for '{Username}', but automatic login failed. Token is null or empty.", RegisterData.Username);
            ErrorMessage = "Registration successful, but automatic login failed. Please try to log in manually.";
            return Page();
        }

        _logger.LogWarning("Registration failed for user '{Username}'. Service indicated a problem.", RegisterData.Username);
        ErrorMessage = "Registration failed. The username might already be in use or there was a service issue. Please try another name.";
        return Page();
    }


    public async Task<IActionResult> OnGetAsync([FromQuery] string? logout)
    {
        _logger.LogInformation("Accessing StartPage (OnGet). 'logout' parameter: {Logout}", logout);

        if (logout == "true")
        {
            _logger.LogInformation("Processing logout request.");
            try
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogDebug("HttpContext.SignOutAsync executed for cookie scheme.");

                _sessionManager.ClearSession();
                _logger.LogDebug("UserSessionManager.ClearSession() executed.");

                Response.Cookies.Delete("DuskSkyToken", new CookieOptions
                {
                    Path = "/",
                    Domain = HttpContext.Request.Host.Host, 
                    SameSite = SameSiteMode.Strict,
                    Secure = HttpContext.Request.IsHttps, 
                    HttpOnly = true
                });
                _logger.LogInformation("Cookie 'DuskSkyToken' deleted.");

                
                foreach (var cookie in Request.Cookies.Keys)
                {
                    if (cookie != ".AspNetCore.Antiforgery" && cookie != ".AspNetCore.Cookies" && cookie != CookieAuthenticationDefaults.AuthenticationScheme)
                    {
                        Response.Cookies.Delete(cookie);
                        _logger.LogDebug("Cookie '{CookieName}' deleted as reinforcement.", cookie);
                    }
                }

                _logger.LogInformation("Logout completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout process.");
                
                TempData["ErrorMessage"] = "There was a problem completely logging you out. Please try again.";
            }
            return RedirectToPage("/StartPage");
        }

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            _logger.LogInformation("User already authenticated, redirecting to /Homepage/Index.");
            return RedirectToPage("/Homepage/Index");
        }

        await LoadHomePageDataAsync();

        return Page();
    }

    private async Task LoadHomePageDataAsync()
    {
        _logger.LogInformation("Loading data for the anonymous homepage.");

        try
        {
            var previews = await _gameService.GetGamePreviewsAsync();
            if (previews == null)
            {
                _logger.LogWarning("GetGamePreviewsAsync returned null. Initializing Games as empty list.");
                Games = new List<GamePreviewDTO>();
            }
            else
            {
                Games = previews.Take(24).ToList();
                _logger.LogInformation("Loaded {Count} game previews.", Games.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading game previews.");
            Games = new List<GamePreviewDTO>(); 
        }

        try
        {
            var recentReviews = await _reviewService.GetRecentReviewsAsync();
            if (recentReviews == null)
            {
                _logger.LogWarning("GetRecentReviewsAsync returned null. Initializing Reviews as empty list.");
                Reviews = new List<ReviewDTO>(); 
            }
            else
            {
                var top10 = recentReviews.Take(10).ToList();
                ReviewImages = new(); 
                var seenGameIds = new HashSet<Guid>(); 

                foreach (var review in top10)
                {
                    if (seenGameIds.Add(review.GameId)) 
                    {
                        try
                        {
                            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
                            if (game != null && !string.IsNullOrEmpty(game.HeaderUrl))
                            {
                                ReviewImages.Add(new ImageReviewDto { HeaderUrl = game.HeaderUrl });
                            }
                            else
                            {
                                _logger.LogWarning("Null/empty game or HeaderUrl for review {ReviewId} (GameId: {GameId}). Using default image.", review.Id, review.GameId);
                                ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
                            }
                        }
                        catch (Exception innerEx)
                        {
                            _logger.LogError(innerEx, "Error getting game preview for review {ReviewId} (GameId: {GameId}).", review.Id, review.GameId);
                            ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
                        }
                    }
                }
                _logger.LogInformation("Loaded {Count} main review images.", ReviewImages.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent reviews or their images.");
            ReviewImages = new List<ImageReviewDto>(); 
        }

        int minReviewImageCards = 10;
        while (ReviewImages.Count < minReviewImageCards)
        {
            ReviewImages.Add(new ImageReviewDto { HeaderUrl = "/Images/noImage.png" });
        }

        try
        {
            var recentReviewsCards = await _reviewService.GetRecentReviewsAsync(6);
            if (recentReviewsCards == null)
            {
                _logger.LogWarning("GetRecentReviewsAsync(6) returned null. Initializing ReviewCards as empty list.");
                ReviewCards = new List<ReviewFullDto>();
            }
            else
            {
                var reviewCardTasks = recentReviewsCards.Select(async review =>
                {
                    string userName = "Unknown User";
                    string avatarUrl = "/Images/noImage.png";
                    string gameTitle = "Unknown Game";
                    string gameImageUrl = "/Images/noImage.png";

                    try
                    {
                        if (!string.IsNullOrEmpty(review.UserId))
                        {
                            var userProfileTask = _userService.GetProfileAsync(review.UserId);
                            var userAuthTask = _authService.SearchUserByIdAsync(review.UserId);

                            await Task.WhenAll(userProfileTask, userAuthTask);

                            var userProfile = userProfileTask.Result;
                            var userAuth = userAuthTask.Result;

                            if (userProfile != null)
                            {
                                userName = userAuth?.Username ?? "Unknown User";
                                avatarUrl = userProfile.AvatarUrl ?? "/Images/noImage.png";
                            }
                            else
                            {
                                _logger.LogWarning("Null user profile for review {ReviewId} (UserId: {UserId}).", review.Id, review.UserId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Null/empty UserId in review {ReviewId}.", review.Id);
                        }

                        if (review.GameId != Guid.Empty) 
                        {
                            var game = await _gameService.GetGamePreviewByIdAsync(review.GameId);
                            if (game != null)
                            {
                                gameTitle = game.Title;
                                gameImageUrl = game.HeaderUrl ?? "/Images/noImage.png";
                            }
                            else
                            {
                                _logger.LogWarning("Null game for review {ReviewId} (GameId: {GameId}).", review.Id, review.GameId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("GameId is Guid.Empty in review {ReviewId}.", review.Id);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error processing user or game details for review {ReviewId}.", review.Id);
                    }

                    return new ReviewFullDto
                    {
                        Id = review.Id,
                        Content = review.Content,
                        Likes = review.Likes,
                        Rating = review.Rating,
                        GameTitle = gameTitle,
                        GameId = review.GameId.ToString(),
                        UserName = userName,
                        ProfileImageUrl = avatarUrl,
                        GameImageUrl = gameImageUrl
                    };
                }).ToList(); 

                ReviewCards = (await Task.WhenAll(reviewCardTasks)).ToList();
                _logger.LogInformation("Loaded {Count} full review cards.", ReviewCards.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading full review cards.");
            ReviewCards = new List<ReviewFullDto>(); 
        }


        try
        {
            var allRecentLists = await _gameListService.GetRecentListsAsync();
            if (allRecentLists == null)
            {
                _logger.LogWarning("GetRecentListsAsync returned null. Initializing RecentLists as empty list.");
                RecentLists = new List<GameListWithUserDto>();
            }
            else
            {
                var publicLists = allRecentLists.Where(list => list.IsPublic).ToList();
                _logger.LogInformation("Found {Count} public lists.", publicLists.Count);

                var listTasks = publicLists.Select(async list =>
                {
                    if (string.IsNullOrWhiteSpace(list.Id))
                    {
                        _logger.LogWarning("List with null/empty ID found. Skipping list processing.");
                        return null; 
                    }

                    string userName = "Unknown User";
                    string avatarUrl = "/Images/noImage.png";
                    var headersUrl = new List<string>();

                    try
                    {
                        var userProfileTask = _userService.GetProfileAsync(list.UserId);
                        var userAuthTask = _authService.SearchUserByIdAsync(list.UserId);
                        var itemsTask = _gameListItemService.GetItemsByListIdAsync(list.Id);

                        await Task.WhenAll(userProfileTask, userAuthTask, itemsTask);

                        var userProfile = userProfileTask.Result;
                        var userAuth = userAuthTask.Result;
                        var items = itemsTask.Result;

                        if (userProfile != null)
                        {
                            userName = userAuth?.Username ?? "Unknown User";
                            avatarUrl = userProfile.AvatarUrl ?? "/Images/noImage.png";
                        }
                        else
                        {
                            _logger.LogWarning("Null user profile for list {ListId} (UserId: {UserId}).", list.Id, list.UserId);
                        }

                        if (items != null)
                        {
                            foreach (var item in items.Take(4))
                            {
                                try
                                {
                                    var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                                    if (game != null && !string.IsNullOrEmpty(game.HeaderUrl))
                                    {
                                        headersUrl.Add(game.HeaderUrl);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("Null/empty game or HeaderUrl for item {GameId} in list {ListId}. Using default image.", item.GameId, list.Id);
                                        headersUrl.Add("/Images/noImage.png");
                                    }
                                }
                                catch (Exception innerGameEx)
                                {
                                    _logger.LogError(innerGameEx, "Error getting game preview {GameId} for list {ListId}.", item.GameId, list.Id);
                                    headersUrl.Add("/Images/noImage.png");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Null list items for list {ListId}. Initializing with empty list for logic.", list.Id);
                        }

                        if (!headersUrl.Any()) 
                        {
                            headersUrl.Add("/Images/noImage.png");
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error processing list {ListId}.", list.Id);
                        return null; 
                    }

                    return new GameListWithUserDto
                    {
                        Id = list.Id,
                        Name = list.Name,
                        Description = list.Description,
                        IsPublic = list.IsPublic,
                        UserId = list.UserId,
                        Date = list.CreatedAt,
                        UserName = userName,
                        AvatarUrl = avatarUrl,
                        GameHeaders = headersUrl
                    };
                }).Where(dto => dto != null); 

                RecentLists = (await Task.WhenAll(listTasks)).ToList()!; 
                _logger.LogInformation("Loaded {Count} recent public lists.", RecentLists.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent lists.");
            RecentLists = new List<GameListWithUserDto>(); 
        }

        _logger.LogInformation("Homepage data loading completed.");
    }
}

