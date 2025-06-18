// Archivo: Pages/Users/Profile/ProfileModelBase.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

public abstract class ProfileModelBase : PageModel
{
    // Propiedades compartidas por todas las páginas del perfil
    public ProfileHeaderViewModel ProfileHeader { get; set; }
    public bool IsOwnProfile { get; set; }
    public FriendshipStatus Friendship { get; set; }
    public string ActiveTab { get; protected set; } // Lo usaremos para resaltar la pestaña activa

    // Método reutilizable para cargar los datos del encabezado
    protected async Task<bool> LoadProfileHeaderData(
        string userId,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService listService)
    {
        var loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwnProfile = (loggedInUserId == userId);

        var authUser = await authService.SearchUserByIdAsync(userId);
        if (authUser == null) return false; // Indica que el usuario no fue encontrado

        var profile = await userManagerService.GetProfileAsync(userId);
        
        var reviewsTask = reviewService.GetFriendsReviewsAsync(new List<string> { userId });
        var listsTask = listService.GetUserListsAsync(userId);
        var friendsTask = friendshipService.GetFriendsAsync(userId);
        await Task.WhenAll(reviewsTask, listsTask, friendsTask);
        
        if (!IsOwnProfile && !string.IsNullOrEmpty(loggedInUserId))
        {
            var receivedRequests = await friendshipService.GetPendingRequestsAsync(loggedInUserId);
            var sentRequestsToProfileUser = await friendshipService.GetPendingRequestsAsync(userId);
            var loggedInUserFriends = await friendshipService.GetFriendsAsync(loggedInUserId);

            if (loggedInUserFriends.Any(f => f.SenderId == userId || f.ReceiverId == userId))
            {
                Friendship = new FriendshipStatus { Status = "friends" };
            }
            else if (receivedRequests.Any(r => r.SenderId == userId))
            {
                Friendship = new FriendshipStatus { Status = "pending_incoming", RequestId = receivedRequests.First(r => r.SenderId == userId).Id };
            }
            else if (sentRequestsToProfileUser.Any(r => r.SenderId == loggedInUserId))
            {
                Friendship = new FriendshipStatus { Status = "pending_outgoing" };
            }
            else
            {
                Friendship = new FriendshipStatus { Status = "not_friends" };
            }
        }

        ProfileHeader = new ProfileHeaderViewModel
        {
            UserId = userId,
            Username = authUser.Username,
            AvatarUrl = profile?.AvatarUrl ?? "/images/default-avatar.png",
            BannerUrl = profile?.BannerUrl ?? "/images/default-banner.jpg",
            Bio = profile?.Bio ?? "No bio available.",
            ReviewCount = reviewsTask.Result.Count,
            ListCount = listsTask.Result.Count,
            FriendCount = friendsTask.Result.Count
        };

        return true; // Indica que el usuario fue encontrado y los datos se cargaron
    }
}