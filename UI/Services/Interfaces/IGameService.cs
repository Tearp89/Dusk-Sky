using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IGameService
{
    Task<List<GamePreviewDTO>> GetGamePreviewsAsync();
    Task<GameDetailsDTO?> GetGameByIdAsync(Guid id);
    Task<(bool success, string? message, Guid? gameId)> ImportGameAsync(int steamAppId);
    Task<GamePreviewDTO?> GetGamePreviewByIdAsync(Guid id);
    Task<List<GameDetailsDTO>> SearchGameDetailsByNameAsync(string name);
    Task<List<GamePreviewDTO>> SearchGamePreviewsByNameAsync(string name);
}
