public class CommentDTO
    {
        public string Id { get; set; } = string.Empty;
        public string ReviewId { get; set; } = string.Empty;
        public string AuthorId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public CommentStatus Status { get; set; }
    }