using System.Text.Json;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;

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
}
