public interface IGameListService
{
    Task<List<GameListDTO>> GetUserListsAsync(string userId);
    Task<GameListDTO?> GetListByIdAsync(string id);
    Task<string?> CreateListAsync(GameListDTO list); // Retorna el Id de la lista
    Task<bool> UpdateListAsync(string id, GameListDTO list);
    Task<bool> DeleteListAsync(string id);
    Task<List<GameListDTO>> GetRecentListsAsync();
    Task<List<GameListDTO>> GetPopularListsAsync();
    Task<bool> LikeListAsync(string id);
    Task<bool> UnlikeListAsync(string id);



    
}