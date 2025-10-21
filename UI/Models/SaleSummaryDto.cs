using System.Text.Json.Serialization;

public class SaleSummaryDto
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public float Price { get; set; }

    [JsonPropertyName("sold")]
    public int Sold { get; set; }

    [JsonPropertyName("available")]
    public int Available { get; set; }
}