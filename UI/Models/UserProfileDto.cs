using System.Text.Json.Serialization;

public class UserProfileDTO
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
    [JsonPropertyName("banner_url")]
    public string? BannerUrl { get; set; }
    public List<MediaItemDTO> Media { get; set; } = new();
    public string? Bio { get; set; }
    public string? AboutSection { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsBanned { get; set; } // true si el usuario está baneado permanentemente
    public DateTime? SuspensionEndDate { get; set; } 
        
    }