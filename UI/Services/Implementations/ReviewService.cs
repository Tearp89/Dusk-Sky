using System.Text.Json;
using System.Web;

public class ReviewService : IReviewService
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions _camelOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };


    public ReviewService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> CreateReviewAsync(ReviewDTO review)
    {
        var response = await _http.PostAsJsonAsync("reviews/", review);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LikeReviewAsync(string reviewId, string userId)
    {
        var response = await _http.PutAsync($"reviews/{reviewId}/like?user_id={userId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnlikeReviewAsync(string reviewId, string userId)
    {
        var response = await _http.PutAsync($"reviews/{reviewId}/unlike?user_id={userId}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteReviewAsync(string reviewId, string userId)
    {
        var requestUri = $"reviews/{reviewId}?user_id={userId}";
        var response = await _http.DeleteAsync(requestUri);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<ReviewDTO>> GetRecentReviewsAsync(int limit = 10)
    {
        return await _http.GetFromJsonAsync<List<ReviewDTO>>("/reviews/recent", _camelOptions) ?? new();
    }

    public async Task<List<ReviewDTO>> GetTopReviewsAsync(int limit = 10)
    {
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/top?limit={limit}") ?? new();
    }

    public async Task<List<ReviewDTO>> GetFriendsReviewsAsync(List<string> friendIds, int limit = 10)
    {
        var query = BuildQuery("friend_ids", friendIds, limit);
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/friends{query}") ?? new();
    }

    public async Task<List<ReviewDTO>> GetReviewsByGameAsync(string gameId)
    {
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/game/{gameId}") ?? new();
    }

    public async Task<List<ReviewDTO>> GetRecentReviewsByGameAsync(string gameId, int limit = 10)
    {
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/game/{gameId}/recent?limit={limit}") ?? new();
    }

    public async Task<List<ReviewDTO>> GetTopReviewsByGameAsync(string gameId, int limit = 10)
    {
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/game/{gameId}/top?limit={limit}") ?? new();
    }

    public async Task<List<ReviewDTO>> GetFriendsReviewsByGameAsync(string gameId, List<string> friendIds, int limit = 10)
    {
        var query = BuildQuery("friend_ids", friendIds, limit);
        return await _http.GetFromJsonAsync<List<ReviewDTO>>($"reviews/game/{gameId}/friends{query}") ?? new();
    }

    public async Task<ReviewDTO?> GetReviewByIdAsync(string reviewId)
    {
        var response = await _http.GetAsync($"reviews/{reviewId}");
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ReviewDTO>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public async Task<bool> HasUserLikedAsync(string reviewId, string userId)
    {
        var review = await GetReviewByIdAsync(reviewId);
        return review?.LikedBy?.Contains(userId) ?? false;
    }


    

    private static string BuildQuery(string key, List<string> values, int limit)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var val in values)
        {
            query.Add(key, val);
        }
        query.Add("limit", limit.ToString());
        return "?" + query.ToString();
    }

    
}