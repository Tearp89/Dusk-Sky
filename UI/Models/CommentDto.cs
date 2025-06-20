using System.Text.Json.Serialization;

public class CommentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        [JsonPropertyName("date")]
        public DateTime CreatedAt { get; set; }
        public CommentStatus Status { get; set; }
    }