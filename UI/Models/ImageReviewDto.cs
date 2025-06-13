using System.Text.Json.Serialization;

public class ImageReviewDto
{
    [JsonPropertyName("headerUrl")]
    public string HeaderUrl { get; set; } 
}