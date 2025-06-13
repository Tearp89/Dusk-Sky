public class TrackingToggleDto
{
    public string GameId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "watch", "like", "watchlist"
    public bool Active { get; set; }
}
