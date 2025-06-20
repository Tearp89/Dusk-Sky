// En tu carpeta de DTOs, archivo SanctionDTO.cs
using System;
using System.Text.Json.Serialization; // <-- ¡Necesario!

public class SanctionDTO
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("report_id")] 
    public string? ReportId { get; set; } 

    [JsonPropertyName("user_id")] 
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("type")] 
    public string Type { get; set; } = string.Empty; 

    [JsonPropertyName("start_date")] 
    public DateTime StartDate { get; set; }

    [JsonPropertyName("end_date")] 
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("reason")] 
    public string Reason { get; set; } = string.Empty;

}