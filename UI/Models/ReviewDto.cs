using System.ComponentModel.DataAnnotations;

public class ReviewDTO
{
    public string Id { get; set; } = string.Empty;
    public Guid GameId { get; set; }
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please write a review.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "The review must be at least 5 characters.")]
    public string Content { get; set; } = string.Empty;

    [Range(0.5, 5, ErrorMessage = "Please select a rating between 0.5 and 5.")]
    public double Rating { get; set; }

    public int Likes { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> LikedBy { get; set; } = new();

    public bool UserLiked(string userId) => LikedBy.Contains(userId);
}
