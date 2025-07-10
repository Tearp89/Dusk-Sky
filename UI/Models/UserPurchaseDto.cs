using System.Text.Json.Serialization;

public class UserPurchaseDto
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; }
}