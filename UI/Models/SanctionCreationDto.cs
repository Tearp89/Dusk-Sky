// En tu backend (moderationservice), tendrías algo así:
using System.Text.Json.Serialization;

public class SanctionCreationDto // O SanctionCreationDTO
{
    // NO incluye 'Id'
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