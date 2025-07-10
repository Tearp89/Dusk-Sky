using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class SalesService : ISalesService
{
    private readonly HttpClient _httpClient;

    public SalesService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string?> CreateSaleAsync(SaleCreateDto sale)
    {
        var content = new StringContent(JsonSerializer.Serialize(sale), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("", content);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var obj = JsonDocument.Parse(json);
            return obj.RootElement.GetProperty("sale_id").GetString();
        }
        return null;
    }

    public async Task<bool> BuyGameAsync(string saleId, string userId)
    {
        var payload = new { user_id = userId };
        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{saleId}/buy", content);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<SaleSummaryDto>> GetSalesSummaryAsync()
    {
        var response = await _httpClient.GetAsync("summary");
        response.EnsureSuccessStatusCode();
        var stream = await response.Content.ReadAsStreamAsync();
        var result = await JsonSerializer.DeserializeAsync<List<SaleSummaryDto>>(stream);
        return result ?? new List<SaleSummaryDto>();
    }

    public async Task<dynamic?> GetSaleByGameIdAsync(string gameId)
    {
        var response = await _httpClient.GetAsync($"game/{gameId}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<dynamic>(json);
        }
        return null;
    }

    public async Task<List<dynamic>> GetPurchasesByUserAsync(string userId)
    {
        var response = await _httpClient.GetAsync($"bought/{userId}");
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<dynamic>>(json) ?? new();
        }
        return new();
    }
}
