using System.Text.Json;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

    public AuthService()
    {
    }

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponseDto?> LoginAsync(AuthRequestDto request)
    {
        var response = await _httpClient.PostAsJsonAsync("login/", request);

        if (response.IsSuccessStatusCode)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var data = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return data;
        }

        return null;
    }


    public async Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("register/", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AuthResponseDto>()
            : null;
    }
    
    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string partialUsername)
    {
        var response = await _httpClient.GetAsync($"users/search?username={Uri.EscapeDataString(partialUsername)}");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<UserSearchResultDto>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<UserSearchResultDto>();
    }

    public async Task<UserSearchResultDto?> SearchUserByIdAsync(string userId)
{
    var response = await _httpClient.GetAsync($"users/{userId}");

    if (response.IsSuccessStatusCode)
    {
        var user = await response.Content.ReadFromJsonAsync<UserSearchResultDto>();
        return user;
    }

    return null;
}

}
