using System.Text.Json.Serialization;

// Este DTO representa el "vínculo de amistad" que viene de la API
public class FriendDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; }

    [JsonPropertyName("receiver_id")]
    public string ReceiverId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("requested_at")]
    public DateTime RequestedAt { get; set; }
}