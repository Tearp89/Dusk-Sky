public interface ICommentService
{
    Task<List<CommentDTO>> GetAllCommentsAsync();
    Task<CommentDTO?> GetCommentByIdAsync(string id);
    Task<CommentDTO?> CreateCommentAsync(CommentDTO comment);
    Task<bool> UpdateCommentStatusAsync(string id, CommentStatus status);
    Task<bool> DeleteCommentAsync(string id);
    Task<List<CommentDTO>> GetCommentsByReviewIdAsync(string reviewId);
}