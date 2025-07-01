using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; 
using System; 

public abstract class ProfileModelBase : PageModel
{
    public ProfileHeaderViewModel ProfileHeader { get; set; }
    public bool IsOwnProfile { get; set; }
    public FriendshipStatus Friendship { get; set; } = new FriendshipStatus { Status = "not_friends", RequestId = null }; 
    public string ActiveTab { get; protected set; } 


    protected IFriendshipService _friendshipService { get; set; } 
    protected IAuthService _authService { get; set; } 


    protected async Task<bool> LoadProfileHeaderData(
        string userId, 
        IAuthService authService, 
        IUserManagerService userManagerService,
        IFriendshipService friendshipService, 
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService) 
    {
        _friendshipService = friendshipService;
        _authService = authService;

        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwnProfile = (loggedInUserId == userId);

        var authUser = await authService.SearchUserByIdAsync(userId); 
        if (authUser == null) return false;

        var profile = await userManagerService.GetProfileAsync(userId);
        
        var reviewsTask = reviewService.GetFriendsReviewsAsync(new List<string> {userId}); 
        var listsTask = listService.GetUserListsAsync(userId);
        var friendsOfProfileUserTask = friendshipService.GetFriendsAsync(userId); 
        await Task.WhenAll(reviewsTask, listsTask, friendsOfProfileUserTask);
        
        var activeFriends = friendsOfProfileUserTask.Result
                                                .Where(f => f.Status == "active") // <-- ¡Cambiamos a 'active' aquí!
                                                .ToList();

        Friendship = await GetFriendshipStatusForProfileAsync(loggedInUserId, userId);

        ProfileHeader = new ProfileHeaderViewModel
        {
            UserId = userId,
            Username = authUser.Username,
            AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png",
            BannerUrl = string.IsNullOrEmpty(profile?.BannerUrl) || profile.BannerUrl.EndsWith(".j_") 
                        ? "/images/default_banner.jpg" 
                        : profile.BannerUrl,
            Bio = profile?.Bio ?? "No bio available.",
            ReviewCount = reviewsTask.Result.Count,
            ListCount = listsTask.Result.Count,
            FriendCount = activeFriends.Count 
        };

        return true;
    }

    private async Task<FriendshipStatus> GetFriendshipStatusForProfileAsync(string? loggedInUserId, string profileUserId)
    {
        if (string.IsNullOrEmpty(loggedInUserId))
            return new FriendshipStatus { Status = "not_friends", RequestId = null };

        if (loggedInUserId == profileUserId)
            return new FriendshipStatus { Status = "self", RequestId = null }; 

        var loggedInUserFriends = await _friendshipService.GetFriendsAsync(loggedInUserId); 
        var myReceivedPendingRequests = await _friendshipService.GetPendingRequestsAsync(loggedInUserId); 

        var profileUserReceivedPendingRequests = await _friendshipService.GetPendingRequestsAsync(profileUserId); 

        if (loggedInUserFriends.Any(f => (f.SenderId == profileUserId && f.ReceiverId == loggedInUserId && f.Status == "accepted") || (f.SenderId == loggedInUserId && f.ReceiverId == profileUserId && f.Status == "accepted")))
        {
            return new FriendshipStatus { Status = "friends", RequestId = null };
        }
        else if (myReceivedPendingRequests.Any(r => r.SenderId == profileUserId && r.ReceiverId == loggedInUserId && r.Status == "pending"))
        {
            var incomingRequest = myReceivedPendingRequests.First(r => r.SenderId == profileUserId && r.ReceiverId == loggedInUserId && r.Status == "pending");
            return new FriendshipStatus { Status = "pending_incoming", RequestId = incomingRequest.Id };
        }
        else if (profileUserReceivedPendingRequests.Any(r => r.SenderId == loggedInUserId && r.ReceiverId == profileUserId && r.Status == "pending"))
        {
            var outgoingRequest = profileUserReceivedPendingRequests.First(r => r.SenderId == loggedInUserId && r.ReceiverId == profileUserId && r.Status == "pending");
            return new FriendshipStatus { Status = "pending_outgoing", RequestId = outgoingRequest.Id }; 
        }
        else
        {
            return new FriendshipStatus { Status = "not_friends", RequestId = null };
        }
    }
}