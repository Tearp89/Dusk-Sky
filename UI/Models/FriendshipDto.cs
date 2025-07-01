using System.Text.Json.Serialization;

public class FriendshipDto
{
    
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("sender_id")] 
    public string SenderId { get; set; }

    [JsonPropertyName("receiver_id")]
    public string ReceiverId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}