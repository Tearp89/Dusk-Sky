// En tu carpeta de DTOs, archivo SanctionDTO.cs
using System;
using System.Text.Json.Serialization; // <-- ¡Necesario!

public class SanctionDTO
{
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string Id { get; set; }

    [JsonPropertyName("ReportId")]
    public string? ReportId { get; set; }

    [JsonPropertyName("UserId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("Type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SanctionType Type { get; set; } // un enum

    
    [JsonPropertyName("StartDate")]
    public DateTime StartDate { get; set; }

    [JsonPropertyName("EndDate")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("Reason")]
    public string Reason { get; set; } = string.Empty;

}

public enum SanctionType
{
    suspension,
    ban
}
