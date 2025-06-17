public class FriendshipStatus
{
    // "friends", "pending_incoming", "pending_outgoing", "not_friends", "is_self"
    public string Status { get; set; }
    public string RequestId { get; set; } // ID necesario para aceptar/rechazar
}