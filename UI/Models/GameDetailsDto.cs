public class GameDetailsDTO
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Developer { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public String ReleaseDate { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new();
    public Dictionary<string, bool> Platforms { get; set; } = new();
    public string HeaderUrl { get; set; } = string.Empty;
    public string? RandomScreenshot { get; set; } // Puede ser nulo si no hay screenshots
    public List<string> AllScreenshots { get; set; } = new();
    

}