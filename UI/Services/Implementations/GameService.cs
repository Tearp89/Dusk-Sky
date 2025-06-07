public class GameService : IGameService
{
    private readonly HttpClient _httpClient;

    public GameService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<GameDto>> GetTopGamesAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<List<GameDto>>("games/top");
        return response ?? new List<GameDto>();
    }

    public async Task<GameDto?> GetGameByIdAsync(Guid gameId)
    {
        return await _httpClient.GetFromJsonAsync<GameDto>($"games/{gameId}");
    }
}
