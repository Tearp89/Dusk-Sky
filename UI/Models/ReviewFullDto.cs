public class ReviewFullDto
{
    public string Id { get; set; } = "";
    public string GameId { get; set; } = "";
    public string GameTitle { get; set; } = "";
    public string GameImageUrl { get; set; } = "";

    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "Unknown";
    public string ProfileImageUrl { get; set; } = "/Images/noImage.png";

    public string Content { get; set; } = "";
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; }

    public int Likes { get; set; }
    public List<string> LikedBy { get; set; } = new();
    public bool UserLiked { get; set; } = false;
}
