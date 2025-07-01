using System.Text.Json.Serialization;

public class FriendshipRequestDTO
{
    [JsonPropertyName("sender_id")]
    public string SenderId { get; set; }

    [JsonPropertyName("receiver_id")]
    public string ReceiverId { get; set; }
}