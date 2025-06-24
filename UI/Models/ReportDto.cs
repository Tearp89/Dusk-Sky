// En tu carpeta de DTOs, archivo ReportDTO.cs
using System;
using System.Text.Json.Serialization; // <-- ¡Necesario!

public class ReportDTO
{
    [JsonPropertyName("id")] // Mapea al campo 'id'
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("reportedUserId")] // Mapea a 'reported_user_id'
    public string ReportedUserId { get; set; } = string.Empty;

    [JsonPropertyName("content_type")] // Mapea a 'content_type' (ej. "comment", "review", "profile")
    public string ContentType { get; set; } = string.Empty; 

    [JsonPropertyName("reason")] // Mapea a 'reason'
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("reportedAt")] // <-- Mapea a 'reported_at' (DateTime)
    public DateTime ReportedAt { get; set; }

    [JsonPropertyName("status")] // Mapea a 'status' (ej. "pending", "resolved")
    public string Status { get; set; } = string.Empty; 

  
}