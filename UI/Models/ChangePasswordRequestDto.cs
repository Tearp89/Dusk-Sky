using System.Text.Json.Serialization; // Necesario para JsonPropertyName

public class ChangePasswordRequestDTO
{
    [JsonPropertyName("current_password")] // Mapea a 'current_password' en JSON
    public string CurrentPassword { get; set; } = string.Empty;

    [JsonPropertyName("new_password")] // Mapea a 'new_password' en JSON
    public string NewPassword { get; set; } = string.Empty;
}