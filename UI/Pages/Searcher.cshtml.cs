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
    public List<SearchUserWithFriendshipStatusDto> Users { get; set; } = new();

    public List<ReviewFullDto> Reviews { get; set; } = new();

    public List<SearchListWithImagesDto> Lists { get; set; } = new();
    private HashSet<string> FriendIds;
    private Dictionary<string, string> IncomingRequestSenders;

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(Query)) return;
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            var friends = await _friendshipService.GetFriendsAsync(currentUserId);
            FriendIds = friends
                .Select(f => f.SenderId == currentUserId ? f.ReceiverId : f.SenderId)
                .ToHashSet();

            var incomingRequests = await _friendshipService.GetPendingRequestsAsync(currentUserId);
            IncomingRequestSenders = incomingRequests.ToDictionary(r => r.SenderId, r => r.Id);
        }

        if (Filter is null or "all")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
            await LoadUsersAsync(Query);
            await LoadReviewsAsync(Query);
            await LoadListsAsync(Query);
        }
        else if (Filter == "games")
        {
            Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
        }
        else if (Filter == "users")
        {
            await LoadUsersAsync(Query);
        }
        else if (Filter == "reviews")
        {
            await LoadReviewsAsync(Query);
        }
        else if (Filter == "lists")
        {
            await LoadListsAsync(Query);
        }
    }

    private async Task LoadUsersAsync(string query)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userMatches = await _authService.SearchUsersAsync(query);

        var userTasks = userMatches.Select(async user =>
        {
            string avatarUrl = "/Images/noImage.png";
            try
            {
                var profile = await _userManagerService.GetProfileAsync(user.Id);
                avatarUrl = profile?.AvatarUrl ?? avatarUrl;
            }
            catch { }
            var friendshipStatus = await GetFriendshipStatus(currentUserId, user.Id);

            return new SearchUserWithFriendshipStatusDto
            {
                Id = user.Id,
                Username = user.Username,
                AvatarUrl = avatarUrl,
                Friendship = friendshipStatus
            };

            
        });

        Users = (await Task.WhenAll(userTasks)).ToList();
    }

    private async Task<FriendshipStatus> GetFriendshipStatus(string currentUserId, string otherUserId)
    {
        if (string.IsNullOrEmpty(currentUserId))
            return new FriendshipStatus { Status = "not_friends" };

        if (currentUserId == otherUserId)
            return new FriendshipStatus { Status = "is_self" };

        if (FriendIds?.Contains(otherUserId) == true)
            return new FriendshipStatus { Status = "friends" };

        if (IncomingRequestSenders?.TryGetValue(otherUserId, out var incomingRequestId) == true)
            return new FriendshipStatus { Status = "pending_incoming", RequestId = incomingRequestId };

        var otherUserPendingRequests = await _friendshipService.GetPendingRequestsAsync(otherUserId);
        var sentRequest = otherUserPendingRequests.FirstOrDefault(r => r.SenderId == currentUserId);
        if (sentRequest != null)
            return new FriendshipStatus { Status = "pending_outgoing", RequestId = sentRequest.Id };

        return new FriendshipStatus { Status = "not_friends" };
    }




   public async Task<IActionResult> OnPostAcceptFriendRequestAsync(string requestId)
{
    if (string.IsNullOrEmpty(requestId))
        return RedirectToPage(new { query = Query, filter = Filter });

    // Calls the service to accept the request
    var result = await _friendshipService.AcceptRequestAsync(requestId);

    if(result)
        TempData["SuccessMessage"] = "Friend added successfully.";
    else
        TempData["ErrorMessage"] = "Could not accept the request.";
    
    // Redirects to the same search page to see the change
    return RedirectToPage(new { query = Query, filter = Filter });
}






    private async Task LoadReviewsAsync(string query)
    {
        var recentReviews = await _reviewService.GetRecentReviewsAsync();
        var matchedGames = await _gameService.SearchGamePreviewsByNameAsync(query);
        var matchedGameIds = matchedGames.Select(g => g.Id).ToHashSet();

        var filtered = recentReviews
            .Where(r =>
                r.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                matchedGameIds.Contains(r.GameId)
            ).ToList();

        var reviewTasks = filtered.Select(async r =>
        {
            string userName = "Unknown";
            string avatarUrl = "/Images/noImage.png";
            string gameTitle = "Unknown Game";
            string gameImage = "/Images/noImage.png";

            var profile = await _userManagerService.GetProfileAsync(r.UserId);
            if (profile != null)
            {
                userName = profile.Username ?? "Unknown";
                avatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
            }

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


    private async Task LoadListsAsync(string query)
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

            var matchedInList = gameItems
                .Where(i => matchedGameIds.Contains(i.GameId))
                .Select(i => matchedGames.FirstOrDefault(g => g.Id == i.GameId)?.HeaderUrl ?? "")
                .Where(url => !string.IsNullOrEmpty(url))
                .Take(6)
                .ToList();

            if (matchedInList.Any())
            {
                dto.GameHeaders = matchedInList;
            }
            else
            {
                dto.GameHeaders = gameItems
                    .Select(i => i.GameId)
                    .Distinct()
                    .Take(6)
                    .Select(async id =>
                    {
                        var game = await _gameService.GetGamePreviewByIdAsync(id);
                        return game?.HeaderUrl ?? "";
                    })
                    .Select(t => t.Result)
                    .Where(url => !string.IsNullOrEmpty(url))
                    .ToList();
            }

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
        TempData["SuccessMessage"] = result ? "Request sended" : "There was a problem sending the request, try again later";
        return RedirectToPage(new { query = Query, filter = Filter });
    }

}
