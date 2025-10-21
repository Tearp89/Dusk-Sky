using System.Text.Json.Serialization;

public class SaleCreateDto
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public float Price { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}
