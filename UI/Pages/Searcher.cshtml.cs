using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class SearcherModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _listService;
    private readonly IUserManagerService _userManagerService;
    private readonly IGameListItemService _listItemService;
    private readonly IFriendshipService _friendshipService;

    public SearcherModel(
        IGameService gameService,
        IAuthService authService,
        IReviewService reviewService,
        IGameListService listService,
        IUserManagerService userManagerService,
        IGameListItemService listItemService,
        IFriendshipService friendshipService)
    {
        _gameService = gameService;
        _authService = authService;
        _reviewService = reviewService;
        _listService = listService;
        _userManagerService = userManagerService;
        _listItemService = listItemService;
        _friendshipService = friendshipService;
    }

    [BindProperty(SupportsGet = true)]
    public string Query { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public List<GamePreviewDTO> Games { get; set; } = new();
    public List<SearchUserWithAvatarDto> Users { get; set; } = new();
    public List<ReviewFullDto> Reviews { get; set; } = new();

    public List<SearchListWithImagesDto> Lists { get; set; } = new();

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;

        if (Filter is null or "all")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
            await CargarUsuariosAsync(Query);
            await CargarReviewsAsync(Query);
            await CargarListasAsync(Query);
        }
        else if (Filter == "games")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
        }
        else if (Filter == "users")
        {
            await CargarUsuariosAsync(Query);
        }
        else if (Filter == "reviews")
        {
            await CargarReviewsAsync(Query);
        }
        else if (Filter == "lists")
        {
            await CargarListasAsync(Query);
        }
    }

    private async Task CargarUsuariosAsync(string query)
    {
        var userMatches = await _authService.SearchUsersAsync(query);
        var userTasks = userMatches.Select(async user =>
        {
            string avatarUrl = "/Images/noImage.png";
            try
            {
                var profile = await _userManagerService.GetProfileAsync(user.Id);
                if (!string.IsNullOrEmpty(profile?.AvatarUrl))
                {
                    avatarUrl = profile.AvatarUrl;
                }
            }
            catch { }

            return new SearchUserWithAvatarDto
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = avatarUrl
            };
        });

        Users = (await Task.WhenAll(userTasks)).ToList();
    }

    private async Task CargarReviewsAsync(string query)
    {
        var recentReviews = await _reviewService.GetRecentReviewsAsync();

        var filtered = recentReviews
            .Where(r => r.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var reviewTasks = filtered.Select(async r =>
        {
            string userName = "Unknown";
            string avatarUrl = "/Images/noImage.png";
            string gameTitle = "Unknown Game";
            string gameImage = "/Images/noImage.png";

            // Obtener datos del perfil
            var profile = await _userManagerService.GetProfileAsync(r.UserId);
            if (profile != null)
            {
                userName = profile.Username ?? "Unknown";
                avatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
            }

            // Obtener datos del juego
            var game = await _gameService.GetGamePreviewByIdAsync(r.GameId);
            if (game != null)
            {
                gameTitle = game.Title;
                gameImage = game.HeaderUrl;
            }

            return new ReviewFullDto
            {
                Id = r.Id,
                GameId = r.GameId.ToString(),
                GameTitle = gameTitle,
                GameImageUrl = gameImage,
                UserId = r.UserId,
                UserName = userName,
                ProfileImageUrl = avatarUrl,
                Content = r.Content,
                Rating = r.Rating,
                CreatedAt = r.CreatedAt,
                Likes = r.Likes,
                LikedBy = r.LikedBy,
                UserLiked = false
            };
        });

        Reviews = (await Task.WhenAll(reviewTasks)).ToList();
    }

    private async Task CargarListasAsync(string query)
    {
        var recentLists = await _listService.GetRecentListsAsync();
        var matchedGames = await _gameService.SearchGamePreviewsByNameAsync(query);
        var matchedGameIds = matchedGames.Select(g => g.Id).ToHashSet();

        var listTasks = recentLists.Select(async list =>
        {
            var dto = new SearchListWithImagesDto
            {
                Id = list.Id,
                Name = list.Name,
                Description = list.Description,
                UserId = list.UserId,
                GameHeaders = new()
            };

            var profile = await _userManagerService.GetProfileAsync(list.UserId);
            if (profile != null)
            {
                dto.UserName = profile.Username ?? "Unknown";
                dto.AvatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
            }

            var gameItems = await _listItemService.GetItemsByListIdAsync(list.Id);
            dto.GameHeaders = gameItems
                .Select(i => i.GameId)
                .Distinct()
                .Take(6)
                .Select(id => matchedGames.FirstOrDefault(g => g.Id == id)?.HeaderUrl ?? "")
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();

            bool nameMatch = (!string.IsNullOrEmpty(dto.Name) && dto.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            bool descMatch = (!string.IsNullOrEmpty(dto.Description) && dto.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
            bool gameMatch = gameItems.Any(i => matchedGameIds.Contains(i.GameId));

            return (matches: nameMatch || descMatch || gameMatch, dto);
        });

        Lists = (await Task.WhenAll(listTasks))
            .Where(t => t.matches)
            .Select(t => t.dto)
            .ToList();
    }
    
    public async Task<IActionResult> OnPostSendFriendRequestAsync(string receiverId)
{
        var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
        return RedirectToPage(); // fallback

    var result = await _friendshipService.SendRequestAsync(senderId, receiverId);
    TempData["SuccessMessage"] = result ? "Solicitud enviada." : "No se pudo enviar la solicitud.";
    return RedirectToPage(new { query = Query, filter = Filter }); // mantiene el contexto de búsqueda
}

}
