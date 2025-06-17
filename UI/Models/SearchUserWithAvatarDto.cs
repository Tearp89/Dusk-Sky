public class SearchUserWithAvatarDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = "/Images/noImage.png";
    public string FriendshipStatus { get; set; } = "none";
    public string? SenderId { get; set; } // para determinar si yo la envié
    public string? ReceiverId { get; set; }
    public string? FriendshipRequestId { get; set; }
    
}
