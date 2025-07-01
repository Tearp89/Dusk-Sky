using System.Text.Json.Serialization;

// Este es el DTO que tu servicio usa, y este es el que necesita ser corregido.
public class FriendRequestDTO
{
    // Con estos atributos, C# sabrá cómo leer el JSON con guiones bajos.
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

    // El campo "_id" del JSON simplemente será ignorado, lo cual está bien.
}