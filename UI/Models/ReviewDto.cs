public class ReviewDTO
    {
        public string Id { get; set; } = string.Empty;
        public Guid GameId { get; set; } 
        public string UserId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Likes { get; set; }
        public float Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }