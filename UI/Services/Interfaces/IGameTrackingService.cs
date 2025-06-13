public interface IGameTrackingService
{
    Task<List<GameTrackingDto>> GetTrackingsByUserAsync(string userId);
    Task<GameTrackingDto?> GetByIdAsync(Guid id);
    Task<GameTrackingDto?> GetByUserAndGameAsync(string userId, string gameId);
    Task<List<string>> GetGameIdsByStatusAsync(string userId, string status);
    Task<List<string>> GetLikedGameIdsAsync(string userId);
    Task<GameTrackingDto?> CreateAsync(GameTrackingDto dto);
    Task<bool> UpdateAsync(Guid id, GameTrackingDto dto);
    Task<bool> DeleteAsync(Guid id);
}
