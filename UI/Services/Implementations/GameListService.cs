public class GameListService : IGameListService
{
    private readonly HttpClient _http;

    public GameListService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GameListDTO>> GetUserListsAsync(string userId)
    {
        return await _http.GetFromJsonAsync<List<GameListDTO>>($"/lists/user/{userId}") ?? new();
    }

    public async Task<GameListDTO?> GetListByIdAsync(string id)
    {
        var response = await _http.GetAsync($"/lists/{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GameListDTO>();
    }

    public async Task<bool> CreateListAsync(GameListDTO list)
    {
        var response = await _http.PostAsJsonAsync("/lists", list);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateListAsync(string id, GameListDTO list)
    {
        var response = await _http.PutAsJsonAsync($"/lists/{id}", list);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteListAsync(string id)
    {
        var response = await _http.DeleteAsync($"/lists/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<GameListDTO>> GetRecentListsAsync()
{
    return await _http.GetFromJsonAsync<List<GameListDTO>>("/lists/recent")
           ?? new List<GameListDTO>();
}

}