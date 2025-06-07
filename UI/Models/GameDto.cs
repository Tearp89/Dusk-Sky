public class GameDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double Rating { get; set; }
    public string? CoverImageUrl { get; set; }
}
