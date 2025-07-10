using System.Text.Json.Serialization;

public class SaleDetailsDto
{
    [JsonPropertyName("_id")] // Correcto, coincide con la base de datos
    public string Id { get; set; }

    [JsonPropertyName("game_id")]
    public string GameId { get; set; }

    [JsonPropertyName("price")]
    public float Price { get; set; }
}