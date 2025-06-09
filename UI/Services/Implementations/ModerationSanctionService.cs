using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ModerationSanctionService : IModerationSanctionService
{
    private readonly HttpClient _http;

    public ModerationSanctionService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<SanctionDTO>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<SanctionDTO>>("/moderation/sanctions") ?? new();
    }

    public async Task<SanctionDTO?> GetByIdAsync(string id)
    {
        var response = await _http.GetAsync($"/moderation/sanctions/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SanctionDTO>()
            : null;
    }

    public async Task<bool> CreateAsync(SanctionDTO sanction)
    {
        var response = await _http.PostAsJsonAsync("/moderation/sanctions", sanction);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAsync(string id, SanctionDTO sanction)
    {
        var response = await _http.PutAsJsonAsync($"/moderation/sanctions/{id}", sanction);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"/moderation/sanctions/{id}");
        return response.IsSuccessStatusCode;
    }
}
