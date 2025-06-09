using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ModerationReportService : IModerationReportService
{
    private readonly HttpClient _http;

    public ModerationReportService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<ReportDTO>> GetAllAsync()
    {
        return await _http.GetFromJsonAsync<List<ReportDTO>>("/moderation/reports") ?? new();
    }

    public async Task<ReportDTO?> GetByIdAsync(string id)
    {
        var response = await _http.GetAsync($"/moderation/reports/{id}");
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<ReportDTO>()
            : null;
    }

    public async Task<bool> CreateAsync(ReportDTO report)
    {
        var response = await _http.PostAsJsonAsync("/moderation/reports", report);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAsync(string id, ReportDTO report)
    {
        var response = await _http.PutAsJsonAsync($"/moderation/reports/{id}", report);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var response = await _http.DeleteAsync($"/moderation/reports/{id}");
        return response.IsSuccessStatusCode;
    }
}
