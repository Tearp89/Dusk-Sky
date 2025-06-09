    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;

    public class GameService : IGameService
    {
        private readonly HttpClient _http;

        public GameService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<GamePreviewDTO>> GetGamePreviewsAsync()
        {
            return await _http.GetFromJsonAsync<List<GamePreviewDTO>>("previews")
                ?? new List<GamePreviewDTO>();
        }

        public async Task<GameDetailsDTO?> GetGameByIdAsync(Guid id)
        {
            var response = await _http.GetAsync($"api/game/{id}");
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<GameDetailsDTO>();
        }

        public async Task<(bool success, string? message, Guid? gameId)> ImportGameAsync(int steamAppId)
        {
            var response = await _http.PostAsync($"api/game/import/{steamAppId}", null);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var conflict = await response.Content.ReadFromJsonAsync<ConflictResponse>();
                return (false, conflict?.message, conflict?.gameId);
            }

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return (false, msg, null);
            }

            var result = await response.Content.ReadFromJsonAsync<ImportSuccessResponse>();
            return (true, result?.message, result?.gameId);
        }
        
       

        private class ConflictResponse
    {
        public string message { get; set; } = string.Empty;
        public Guid gameId { get; set; }
    }

        private class ImportSuccessResponse
        {
            public string message { get; set; } = string.Empty;
            public Guid gameId { get; set; }
        }
    }
