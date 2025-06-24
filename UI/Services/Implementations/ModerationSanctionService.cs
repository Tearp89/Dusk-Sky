using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class ModerationSanctionService : IModerationSanctionService
{
    private readonly HttpClient _http;

    public ModerationSanctionService(HttpClient http)
    {
        _http = http;
    }

    public ModerationSanctionService()
    {
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
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
            WriteIndented = true
        };

        string json = JsonSerializer.Serialize(sanction, options);
        Console.WriteLine("🔍 Payload enviado al microservicio:");
        Console.WriteLine(json);
        var response = await _http.PostAsJsonAsync("/moderation/sanctions", sanction, options);
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

    public async Task<List<SanctionDTO>> GetActiveSanctionsForUserAsync(string userId)
    {
        // 1. Obtener todas las sanciones
        List<SanctionDTO> allSanctions = await GetAllAsync(); // Reutilizamos el método existente GetAllAsync()

        if (allSanctions != null)
        {
            // 2. Filtrar por el ID de usuario y por aquellas que estén activas
            var activeSanctionsForUser = allSanctions
                .Where(s => s.UserId == userId && s.IsActive()) // Usamos el método de extensión IsActive()
                .ToList();

            return activeSanctionsForUser;
        }
        return new List<SanctionDTO>();
    }

    public async Task<bool> HasActiveSanctionAsync(string userId)
    {
        var sanctions = await GetAllAsync(); // Replace with endpoint that only fetches user's sanctions if possible
        var now = DateTime.UtcNow;

        return sanctions.Any(s =>
            s.UserId == userId &&
            (
                s.Type == SanctionType.ban ||
                (s.Type == SanctionType.suspension && s.StartDate <= now && s.EndDate >= now)
            ));
    }




}
