using System.Net.Http.Json;

public class GameTrackingService : IGameTrackingService
{
    private readonly HttpClient _httpClient;

    public GameTrackingService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GameTrackingDto>> GetTrackingsByUserAsync(string userId)
    {
        var response = await _httpClient.GetFromJsonAsync<List<GameTrackingDto>>($"api/trackings/user/{userId}");
        return response ?? new List<GameTrackingDto>();
    }

    public async Task<GameTrackingDto?> GetByIdAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<GameTrackingDto>($"api/trackings/{id}");
    }

    public async Task<GameTrackingDto?> GetByUserAndGameAsync(string userId, string gameId)
    {
        var response = await _httpClient.PostAsJsonAsync("api/trackings/lookup", new { UserId = userId, GameId = gameId });

        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GameTrackingDto>();

        return null;
    }

    public async Task<List<string>> GetGameIdsByStatusAsync(string userId, string status)
    {
        var result = await _httpClient.GetFromJsonAsync<List<string>>($"api/trackings/user/{userId}/status/{status}");
        return result ?? new List<string>();
    }

    public async Task<List<string>> GetLikedGameIdsAsync(string userId)
    {
        var result = await _httpClient.GetFromJsonAsync<List<string>>($"api/trackings/user/{userId}/liked");
        return result ?? new List<string>();
    }

    public async Task<GameTrackingDto?> CreateAsync(GameTrackingDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("api/trackings", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<GameTrackingDto>();
        return null;
    }

    public async Task<bool> UpdateAsync(Guid id, GameTrackingDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/trackings/{id}", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"api/trackings/{id}");
        return response.IsSuccessStatusCode;
    }
}
