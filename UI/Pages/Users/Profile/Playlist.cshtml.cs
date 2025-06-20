using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;

public class PlaylistProfileModel : ProfileModelBase
{
    // --- Servicios inyectados ---
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _gameListService;

    // --- Propiedad para la lista de juegos en "backlog" ---
    public List<GamePreviewDTO> BacklogGames { get; set; } = new();

    public PlaylistProfileModel(
        IGameTrackingService gameTrackingService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService gameListService)
    {
        _gameTrackingService = gameTrackingService;
        _gameService = gameService;
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _reviewService = reviewService;
        _gameListService = gameListService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Playlist"; // Para que la pestaña "Playlist" se resalte

        // 1. Cargar los datos del encabezado del perfil
        var userExists = await LoadProfileHeaderData(
            userId,
            _authService,
            _userManagerService,
            _friendshipService,
            _reviewService,
            _gameListService
            , _gameTrackingService  ); 

        if (!userExists)
        {
            return NotFound();
        }

        // 2. Obtener los IDs de los juegos en "backlog"
        // Asumo que tu GameTrackingService.GetGameIdsByStatusAsync puede filtrar por "backlog"
        var backlogGameIdsStrings = await _gameTrackingService.GetGameIdsByStatusAsync(userId, "backlog");
        
        // Convertir los IDs de string a Guid y filtrarlos si son inválidos
        var backlogGameIds = backlogGameIdsStrings
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        // 3. Obtener los detalles de los juegos en "backlog" de forma concurrente
        var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
        foreach (var gameId in backlogGameIds)
        {
            gamePreviewTasks.Add(_gameService.GetGamePreviewByIdAsync(gameId));
        }

        BacklogGames = (await Task.WhenAll(gamePreviewTasks))
            .Where(gp => gp != null).ToList()!;
        
        return Page();
    }
}