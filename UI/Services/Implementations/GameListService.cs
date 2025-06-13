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

    public async Task<string?> CreateListAsync(GameListDTO list)
    {
        var response = await _http.PostAsJsonAsync("/lists", list);
        if (!response.IsSuccessStatusCode)
            return null;

        var created = await response.Content.ReadFromJsonAsync<GameListDTO>();
        return created?.Id;
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

    public async Task<List<GameListDTO>> GetPopularListsAsync()
    {
        return await _http.GetFromJsonAsync<List<GameListDTO>>("/lists/popular")
               ?? new List<GameListDTO>();
    }

    public async Task<bool> LikeListAsync(string id)
    {
        var response = await _http.PutAsync($"/lists/like/{id}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnlikeListAsync(string id)
    {
        var response = await _http.PutAsync($"/lists/unlike/{id}", null);
        return response.IsSuccessStatusCode;
    }


}