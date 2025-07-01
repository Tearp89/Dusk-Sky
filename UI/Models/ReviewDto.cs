using System.ComponentModel.DataAnnotations;

public class ReviewDTO
{
    public string Id { get; set; } = string.Empty;
    public Guid GameId { get; set; }
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Por favor escribe una review.")]
    [StringLength(1000, MinimumLength = 5, ErrorMessage = "La review debe de tener al menos 5 caracteres.")]
    public string Content { get; set; } = string.Empty;

    [Range(0.5, 5, ErrorMessage = "Por favor elige una calificación entre 1 y 5")]
    public double Rating { get; set; }

    public int Likes { get; set; }

    public DateTime CreatedAt { get; set; }

    public List<string> LikedBy { get; set; } = new();

    public bool UserLiked(string userId) => LikedBy.Contains(userId);
}
