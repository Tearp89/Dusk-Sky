using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// Make sure your using statements point to your DTOs and Services
// using YourApp.Services;
// using YourApp.DTOs;

// ViewModel for displaying an accepted friend


// ViewModel for displaying a pending friend request


public class FriendsModel : PageModel
{
    private readonly IFriendshipService _friendshipService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;

    // Properties to hold the data for each tab
    public List<FriendViewModel> Friends { get; set; } = new();
    public List<FriendRequestViewModel> PendingRequests { get; set; } = new();

    public FriendsModel(
        IFriendshipService friendshipService,
        IAuthService authService,
        IUserManagerService userManagerService)
    {
        _friendshipService = friendshipService;
        _authService = authService;
        _userManagerService = userManagerService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Challenge(); // Or redirect to login
        }

        // --- Load Friends List ---
        var friendDocs = await _friendshipService.GetFriendsAsync(currentUserId);
        var friendTasks = friendDocs.Select(async f =>
        {
            var friendId = f.SenderId == currentUserId ? f.ReceiverId : f.SenderId;
            var authUser = await _authService.SearchUserByIdAsync(friendId); // Assuming you have a method like this
            var profile = await _userManagerService.GetProfileAsync(friendId);
            return new FriendViewModel
            {
                UserId = friendId,
                Username = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png"
            };
        });
        Friends = (await Task.WhenAll(friendTasks)).ToList();

        // --- Load Pending Requests List ---
        var requestDocs = await _friendshipService.GetPendingRequestsAsync(currentUserId);
        var requestTasks = requestDocs.Select(async r =>
        {
            var senderId = r.SenderId;
            var authUser = await _authService.SearchUserByIdAsync(senderId);
            var profile = await _userManagerService.GetProfileAsync(senderId);
            return new FriendRequestViewModel
            {
                RequestId = r.Id,
                UserId = senderId,
                Username = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                AvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png"
            };
        });
        PendingRequests = (await Task.WhenAll(requestTasks)).ToList();

        return Page();
    }

    // Handler for the "Accept" button
    public async Task<IActionResult> OnPostAcceptRequestAsync(string requestId)
    {
        await _friendshipService.AcceptRequestAsync(requestId);
        return RedirectToPage(); // Reload the same page
    }

    // Handler for the "Reject" button
    public async Task<IActionResult> OnPostRejectRequestAsync(string requestId)
    {
        await _friendshipService.RejectRequestAsync(requestId);
        return RedirectToPage(); // Reload the same page
    }
}