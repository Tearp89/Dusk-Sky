using System.Text.Json.Serialization;

public class GamePreviewWithNotesDto
{
    public GamePreviewDTO Game { get; set; } = null!;
    [JsonPropertyName("comment")]
    public string Notes { get; set; } = string.Empty;
}
