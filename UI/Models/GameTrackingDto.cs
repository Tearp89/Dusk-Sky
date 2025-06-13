public class GameTrackingDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // playing, completed, etc.
    public bool Liked { get; set; }
}
