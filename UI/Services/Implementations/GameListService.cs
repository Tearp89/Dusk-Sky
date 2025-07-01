public class GameListService : IGameListService
{
    private readonly HttpClient _http;

    public GameListService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<GameListDTO>> GetUserListsAsync(string userId)
    {
        return await _http.GetFromJsonAsync<List<GameListDTO>>($"user/{userId}") ?? new();
    }

    public async Task<GameListDTO?> GetListByIdAsync(string id)
    {
        var response = await _http.GetAsync($"{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GameListDTO>();
    }

    public async Task<string?> CreateListAsync(GameListDTO list)
    {
        var response = await _http.PostAsJsonAsync("", list);
        if (!response.IsSuccessStatusCode)
            return null;

        var created = await response.Content.ReadFromJsonAsync<GameListDTO>();
        return created?.Id;
    }


    public async Task<bool> UpdateListAsync(string id, GameListDTO list)
    {
        var response = await _http.PutAsJsonAsync($"{id}", list);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteListAsync(string id)
    {
        var response = await _http.DeleteAsync($"{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<GameListDTO>> GetRecentListsAsync()
    {
        return await _http.GetFromJsonAsync<List<GameListDTO>>("recent")
               ?? new List<GameListDTO>();
    }

    public async Task<List<GameListDTO>> GetPopularListsAsync()
    {
        return await _http.GetFromJsonAsync<List<GameListDTO>>("popular")
               ?? new List<GameListDTO>();
    }

    public async Task<bool> LikeListAsync(string id)
    {
        var response = await _http.PutAsync($"like/{id}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnlikeListAsync(string id)
    {
        var response = await _http.PutAsync($"unlike/{id}", null);
        return response.IsSuccessStatusCode;
    }


}