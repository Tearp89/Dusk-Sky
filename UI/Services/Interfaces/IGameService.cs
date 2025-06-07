public interface IGameService
{
    Task<List<GameDto>> GetTopGamesAsync();
    Task<GameDto?> GetGameByIdAsync(Guid gameId);
}
