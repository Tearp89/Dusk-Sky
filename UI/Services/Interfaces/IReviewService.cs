public interface IReviewService
{
    Task<bool> CreateReviewAsync(ReviewDTO review);
    Task<bool> LikeReviewAsync(string reviewId, string userId);
    Task<bool> UnlikeReviewAsync(string reviewId, string userId);
    Task<bool> DeleteReviewAsync(string reviewId, string userId);
    Task<List<ReviewDTO>> GetRecentReviewsAsync(int limit = 10);
    Task<List<ReviewDTO>> GetTopReviewsAsync(int limit = 10);
    Task<List<ReviewDTO>> GetFriendsReviewsAsync(List<string> friendIds, int limit = 10);
    Task<List<ReviewDTO>> GetReviewsByGameAsync(string gameId);
    Task<List<ReviewDTO>> GetRecentReviewsByGameAsync(string gameId, int limit = 10);
    Task<List<ReviewDTO>> GetTopReviewsByGameAsync(string gameId, int limit = 10);
    Task<List<ReviewDTO>> GetFriendsReviewsByGameAsync(string gameId, List<string> friendIds, int limit = 10);
    Task<bool> HasUserLikedAsync(string reviewId, string userId);
    Task<ReviewDTO?> GetReviewByIdAsync(string reviewId);
}