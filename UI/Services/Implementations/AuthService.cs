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

    public async Task<bool> ChangePasswordAsync(string userId, string oldPassword, string newPassword)
    {
        // El endpoint de FastAPI es POST /change-password/{user_id}
        // y espera un ChangePasswordRequest en el cuerpo.
        var requestPayload = new ChangePasswordRequestDTO // El DTO de C# para el request body
        {
            CurrentPassword = oldPassword,
            NewPassword = newPassword
        };

        var response = await _httpClient.PostAsJsonAsync($"change-password/{userId}", requestPayload);
        // Tu FastAPI devuelve 200 OK si es exitoso.
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAccountAsync(string userId)
    {
        // El endpoint de FastAPI es DELETE /delete/{user_id}
        var response = await _httpClient.DeleteAsync($"delete/{userId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateUsernameAsync(string userId, string newUsername)
    {
        // El endpoint de FastAPI es PUT /update-username/{user_id}
        // y espera el new_username como Query Parameter (o en el cuerpo si fuera JSON).
        // Según tu endpoint: `@router.put("/update-username/{user_id}") async def update_username(user_id: str, new_username: str)`
        // Aquí new_username es un parámetro de consulta, no parte del cuerpo JSON.
        // Si fuera un JSON, sería await _httpClient.PutAsJsonAsync(...) con un DTO.
        // Como es Query Param, lo añadimos a la URL.
        var response = await _httpClient.PutAsync($"update-username/{userId}?new_username={Uri.EscapeDataString(newUsername)}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> PromoteUserAsync(string userId)
    {
        // El endpoint de FastAPI es PUT /promote/{user_id} y no espera cuerpo de solicitud.
        // Se usa PutAsync con 'null' para el cuerpo.
        var response = await _httpClient.PutAsync($"promote/{userId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DemoteUserAsync(string userId)
    {
        // El endpoint de FastAPI es PUT /demote/{user_id} y no espera cuerpo de solicitud.
        // Se usa PutAsync con 'null' para el cuerpo.
        var response = await _httpClient.PutAsync($"demote/{userId}", null);
        return response.IsSuccessStatusCode;
    }

}
