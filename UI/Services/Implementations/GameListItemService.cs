public class GameListItemService : IGameListItemService
{
    private readonly HttpClient _http;

    public GameListItemService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GameListItemDTO>> GetItemsByListIdAsync(string listId)
    {
        return await _http.GetFromJsonAsync<List<GameListItemDTO>>($"/lists/{listId}/items") ?? new();
    }

    public async Task<bool> AddItemAsync(GameListItemDTO item)
    {
        var response = await _http.PostAsJsonAsync($"/lists/{item.ListId}/items", item); // ✅ usa la ruta con listId
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateItemAsync(GameListItemDTO item)
    {
        var response = await _http.PutAsJsonAsync("/lists/items", item);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteItemAsync(string listId, string itemId)
{
    var response = await _http.DeleteAsync($"/lists/{listId}/items/{itemId}");
    return response.IsSuccessStatusCode;
}


    public async Task<bool> ExistsAsync(string listId, Guid gameId)
{
    var items = await GetItemsByListIdAsync(listId);
    return items.Any(i => i.GameId == gameId);
}

}