using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

public class FriendshipService : IFriendshipService
{
    private readonly HttpClient _http;

    public FriendshipService(HttpClient http)
    {
        _http = http;
    }

    public async Task<bool> SendRequestAsync(string senderId, string receiverId)
    {
        var request = new FriendshipRequestDTO
        {
            SenderId = senderId,
            ReceiverId = receiverId
        };

        var response = await _http.PostAsJsonAsync("/friendships/", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> AcceptRequestAsync(string requestId)
    {
        var response = await _http.PutAsync($"/friendships/{requestId}/accept", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectRequestAsync(string requestId)
    {
        var response = await _http.PutAsync($"/friendships/{requestId}/reject", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<FriendDto>> GetFriendsAsync(string userId)
    {
        return await _http.GetFromJsonAsync<List<FriendDto>>($"/friendships/user/{userId}") ?? new();
    }

   // Dentro de tu clase FriendshipService.cs

// Dentro de tu clase FriendshipService.cs

public async Task<List<FriendRequestDTO>> GetPendingRequestsAsync(string userId)
{
    return await _http.GetFromJsonAsync<List<FriendRequestDTO>>($"/friendships/pending/{userId}") ?? new();
}
}
