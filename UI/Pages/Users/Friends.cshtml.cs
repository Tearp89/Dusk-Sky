using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System; // For ArgumentNullException
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Make sure this using is present
using System.Net.Http; // ✅ Add this using for HttpRequestException


public class FriendsModel : PageModel
{
    private readonly IFriendshipService _friendshipService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly ILogger<FriendsModel> _logger; 

    public List<FriendViewModel> Friends { get; set; } = new();
    public List<FriendRequestViewModel> PendingRequests { get; set; } = new();

    public FriendsModel(
        IFriendshipService friendshipService,
        IAuthService authService,
        IUserManagerService userManagerService,
        ILogger<FriendsModel> logger) // ✅ Inject ILogger
    {
        _friendshipService = friendshipService ?? throw new ArgumentNullException(nameof(friendshipService), "IFriendshipService cannot be null.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService cannot be null.");
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService cannot be null.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger cannot be null."); 
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("OnGetAsync: User not authenticated. Redirecting to Challenge."); 
            TempData["ErrorMessage"] = "You must be logged in to view your friends.";
            return Challenge(); 
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Loading friends and pending requests for user '{UserId}'.", currentUserId); 

            var friendDocs = await _friendshipService.GetFriendsAsync(currentUserId);
            friendDocs ??= new List<FriendDto>(); 

            var friendTasks = friendDocs.Select(async f =>
            {
                if (f == null) 
                {
                    _logger.LogWarning("OnGetAsync: Found a null friend document for user '{UserId}'. Skipping.", currentUserId); 
                    return null;
                }

                var friendId = f.SenderId == currentUserId ? f.ReceiverId : f.SenderId;
                if (string.IsNullOrWhiteSpace(friendId)) 
                {
                    _logger.LogWarning("OnGetAsync: Friend ID is null/whitespace for document ID '{FriendshipDocId}'. Skipping.", f.Id); 
                    return null;
                }

                try
                {
                    var authUser = await _authService.SearchUserByIdAsync(friendId);

                    if (authUser != null && authUser.status == "deleted")
                    {
                        return null;
                    }
                    var profile = await _userManagerService.GetProfileAsync(friendId);

                    return new FriendViewModel
                    {
                        UserId = friendId,
                        Username = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png"
                    };
                }
                catch (HttpRequestException ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: HttpRequestException loading friend data for ID '{FriendId}' of user '{UserId}'. Message: {Message}", friendId, currentUserId, ex.Message); 
                    return null; 
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: Unexpected error loading friend data for ID '{FriendId}' of user '{UserId}'. Message: {Message}", friendId, currentUserId, ex.Message); 
                    return null;
                }
            });
            Friends = (await Task.WhenAll(friendTasks)).Where(f => f != null).ToList()!; 
            _logger.LogInformation("OnGetAsync: Loaded {Count} friends for user '{UserId}'.", Friends.Count, currentUserId); 

            var requestDocs = await _friendshipService.GetPendingRequestsAsync(currentUserId);
            requestDocs ??= new List<FriendRequestDTO>(); 

            var requestTasks = requestDocs.Select(async r =>
            {
                if (r == null) 
                {
                    _logger.LogWarning("OnGetAsync: Found a null pending request document for user '{UserId}'. Skipping.", currentUserId); 
                    return null;
                }

                var senderId = r.SenderId;
                if (string.IsNullOrWhiteSpace(senderId)) 
                {
                    _logger.LogWarning("OnGetAsync: Sender ID is null/whitespace for request ID '{RequestId}'. Skipping.", r.Id); 
                    return null;
                }

                try
                {
                    var authUser = await _authService.SearchUserByIdAsync(senderId);
                    var profile = await _userManagerService.GetProfileAsync(senderId);
                    return new FriendRequestViewModel
                    {
                        RequestId = r.Id,
                        UserId = senderId,
                        Username = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                        AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png"
                    };
                }
                catch (HttpRequestException ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: HttpRequestException loading request sender data for ID '{SenderId}' of user '{UserId}'. Message: {Message}", senderId, currentUserId, ex.Message); 
                    return null;
                }
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "OnGetAsync: Unexpected error loading request sender data for ID '{SenderId}' of user '{UserId}'. Message: {Message}", senderId, currentUserId, ex.Message); 
                    return null;
                }
            });
            PendingRequests = (await Task.WhenAll(requestTasks)).Where(r => r != null).ToList()!; // ✅ Filter out nulls
            _logger.LogInformation("OnGetAsync: Loaded {Count} pending requests for user '{UserId}'.", PendingRequests.Count, currentUserId); 

            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException while loading friends/requests for user '{UserId}'. Message: {Message}", currentUserId, ex.Message); 
            TempData["ErrorMessage"] = $"An argument error occurred: {ex.Message}";
            Friends = new List<FriendViewModel>();
            PendingRequests = new List<FriendRequestViewModel>();
            return Page();
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException while loading friends/requests for user '{UserId}'. Message: {Message}", currentUserId, ex.Message); 
            TempData["ErrorMessage"] = $"An operational error occurred: {ex.Message}";
            Friends = new List<FriendViewModel>();
            PendingRequests = new List<FriendRequestViewModel>();
            return Page();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: General HttpRequestException while loading friends/requests for user '{UserId}'. Message: {Message}", currentUserId, ex.Message); 
            TempData["ErrorMessage"] = "A connection issue occurred while loading your friends. Please check your internet connection.";
            Friends = new List<FriendViewModel>();
            PendingRequests = new List<FriendRequestViewModel>();
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: An unexpected error occurred while loading friends/requests for user '{UserId}'. Message: {Message}", currentUserId, ex.Message); 
            TempData["ErrorMessage"] = "An unexpected error occurred while loading your friends. Please try again later.";
            Friends = new List<FriendViewModel>();
            PendingRequests = new List<FriendRequestViewModel>();
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAcceptRequestAsync(string requestId)
    {
        // ✅ Validate requestId
        if (string.IsNullOrWhiteSpace(requestId))
        {
            _logger.LogWarning("OnPostAcceptRequestAsync: Request ID is null or empty."); 
            TempData["StatusMessage"] = "Error: Invalid request ID.";
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("OnPostAcceptRequestAsync: User not authenticated for request ID '{RequestId}'.", requestId); 
            return Challenge();
        }

        try
        {
            _logger.LogInformation("OnPostAcceptRequestAsync: User '{UserId}' attempting to accept request '{RequestId}'.", currentUserId, requestId); 
            var success = await _friendshipService.AcceptRequestAsync(requestId);
            if (success)
            {
                _logger.LogInformation("OnPostAcceptRequestAsync: Request '{RequestId}' accepted by user '{UserId}'.", requestId, currentUserId); 
                TempData["StatusMessage"] = "Friend request accepted! You are now friends.";
            }
            else
            {
                _logger.LogWarning("OnPostAcceptRequestAsync: Failed to accept request '{RequestId}' by user '{UserId}'. Possible duplicate or not found.", requestId, currentUserId); 
                TempData["StatusMessage"] = "Error: Failed to accept friend request.";
            }
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostAcceptRequestAsync: HttpRequestException accepting request '{RequestId}' by user '{UserId}'. Message: {Message}", requestId, currentUserId, ex.Message); 
            TempData["StatusMessage"] = "Error: A connection issue occurred while accepting the request. Please try again.";
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAcceptRequestAsync: Unexpected error accepting request '{RequestId}' by user '{UserId}'. Message: {Message}", requestId, currentUserId, ex.Message); 
            TempData["StatusMessage"] = "Error: An unexpected error occurred while accepting the request.";
        }
        return RedirectToPage(); 
    }

    public async Task<IActionResult> OnPostRejectRequestAsync(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            _logger.LogWarning("OnPostRejectRequestAsync: Request ID is null or empty."); 
            TempData["StatusMessage"] = "Error: Invalid request ID.";
            return BadRequest();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogWarning("OnPostRejectRequestAsync: User not authenticated for request ID '{RequestId}'.", requestId); 
            return Challenge();
        }

        try
        {
            _logger.LogInformation("OnPostRejectRequestAsync: User '{UserId}' attempting to reject request '{RequestId}'.", currentUserId, requestId); 
            var success = await _friendshipService.RejectRequestAsync(requestId);
            if (success)
            {
                _logger.LogInformation("OnPostRejectRequestAsync: Request '{RequestId}' rejected by user '{UserId}'.", requestId, currentUserId); 
                TempData["StatusMessage"] = "Friend request rejected.";
            }
            else
            {
                _logger.LogWarning("OnPostRejectRequestAsync: Failed to reject request '{RequestId}' by user '{UserId}'. Possible not found.", requestId, currentUserId); 
                TempData["StatusMessage"] = "Error: Failed to reject friend request.";
            }
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostRejectRequestAsync: HttpRequestException rejecting request '{RequestId}' by user '{UserId}'. Message: {Message}", requestId, currentUserId, ex.Message); 
            TempData["StatusMessage"] = "Error: A connection issue occurred while rejecting the request. Please try again.";
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostRejectRequestAsync: Unexpected error rejecting request '{RequestId}' by user '{UserId}'. Message: {Message}", requestId, currentUserId, ex.Message); 
            TempData["StatusMessage"] = "Error: An unexpected error occurred while rejecting the request.";
        }
        return RedirectToPage(); 
    }
}