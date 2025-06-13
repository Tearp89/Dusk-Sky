public interface IGameListService
{
    Task<List<GameListDTO>> GetUserListsAsync(string userId);
    Task<GameListDTO?> GetListByIdAsync(string id);
    Task<bool> CreateListAsync(GameListDTO list);
    Task<bool> UpdateListAsync(string id, GameListDTO list);
    Task<bool> DeleteListAsync(string id);
    Task<List<GameListDTO>> GetRecentListsAsync();

    
}