public interface IGameListItemService
{
    Task<List<GameListItemDTO>> GetItemsByListIdAsync(string listId);
    Task<bool> AddItemAsync(GameListItemDTO item);
    Task<bool> UpdateItemAsync(GameListItemDTO item);
    Task<bool> DeleteItemAsync(string itemId);
}