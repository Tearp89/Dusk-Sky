public class ReviewWithUserDto
{
    public string Id { get; set; } = string.Empty;
    public Guid GameId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Likes { get; set; }
    public DateTime CreatedAt { get; set; }
    public float Rating { get; set; }
    public string UserName { get; set; } = "Usuario desconocido";
    public string ProfileImageUrl { get; set; } = "/Images/noImage.png";

    public string GameImageUrl { get; set; } = "/Images/noImage.png";

    public List<string> LikedBy { get; set; }

    public bool UserLiked { get; set; }
}
