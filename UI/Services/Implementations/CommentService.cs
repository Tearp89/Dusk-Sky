using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class CommentService : ICommentService
{
    private readonly HttpClient _http;

    // Opciones de serialización para enums como strings (e.g., "visible")
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public CommentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CommentDTO>> GetAllCommentsAsync()
    {
        return await _http.GetFromJsonAsync<List<CommentDTO>>("/comments", _jsonOptions) ?? new();
    }

    public async Task<CommentDTO?> GetCommentByIdAsync(string id)
    {
        var response = await _http.GetAsync($"/comments/{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CommentDTO>(_jsonOptions);
    }

    public async Task<CommentDTO?> CreateCommentAsync(CommentDTO comment)
    {
        var response = await _http.PostAsJsonAsync("/comments", comment, _jsonOptions);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CommentDTO>(_jsonOptions);
    }

    public async Task<bool> UpdateCommentStatusAsync(string id, CommentStatus status)
    {
        var response = await _http.PutAsJsonAsync($"/comments/{id}", status, _jsonOptions);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCommentAsync(string id)
    {
        var response = await _http.DeleteAsync($"/comments/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CommentDTO>> GetCommentsByReviewIdAsync(string reviewId)
    {
        return await _http.GetFromJsonAsync<List<CommentDTO>>($"/comments/review/{reviewId}", _jsonOptions)
               ?? new();
    }
}
