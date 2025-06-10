using System.Text.Json.Serialization;

// Paso 1: Ajusta el DTO
public class GamePreviewDTO
{
    public Guid Id { get; set; }

    [JsonPropertyName("headerUrl")]
    public string HeaderUrl { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Title { get; set; } = string.Empty;
}
