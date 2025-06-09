using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class CommentService : ICommentService
{
    private readonly HttpClient _http;

    public CommentService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<CommentDTO>> GetAllCommentsAsync()
    {
        return await _http.GetFromJsonAsync<List<CommentDTO>>("/comments") ?? new();
    }

    public async Task<CommentDTO?> GetCommentByIdAsync(string id)
    {
        var response = await _http.GetAsync($"/comments/{id}");
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CommentDTO>();
    }

    public async Task<CommentDTO?> CreateCommentAsync(CommentDTO comment)
    {
        var response = await _http.PostAsJsonAsync("/comments", comment);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CommentDTO>();
    }

    public async Task<bool> UpdateCommentStatusAsync(string id, CommentStatus status)
    {
        var response = await _http.PutAsJsonAsync($"/comments/{id}", status);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCommentAsync(string id)
    {
        var response = await _http.DeleteAsync($"/comments/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<List<CommentDTO>> GetCommentsByReviewIdAsync(string reviewId)
    {
        return await _http.GetFromJsonAsync<List<CommentDTO>>($"/comments/review/{reviewId}") ?? new();
    }
}
