public interface IFriendshipService
{
    Task<bool> SendRequestAsync(string senderId, string receiverId);
    Task<bool> AcceptRequestAsync(string requestId);
    Task<bool> RejectRequestAsync(string requestId);
    Task<List<FriendDto>> GetFriendsAsync(string userId);
    Task<List<FriendRequestDTO>> GetPendingRequestsAsync(string userId);
}