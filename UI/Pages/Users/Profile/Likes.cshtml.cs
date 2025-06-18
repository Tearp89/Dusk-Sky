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

public class LikesModel : ProfileModelBase
{
    // --- Servicios inyectados ---
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IReviewService _reviewService; // Para LoadProfileHeaderData
    private readonly IGameListService _gameListService; // Para LoadProfileHeaderData


    // --- Propiedad para la lista de juegos "likeados" ---
    public List<GamePreviewDTO> LikedGames { get; set; } = new();

    public LikesModel(
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
        ActiveTab = "Likes"; // Para que la pestaña "Likes" se resalte

        // 1. Cargar los datos del encabezado del perfil
        var userExists = await LoadProfileHeaderData(
            userId,
            _authService,
            _userManagerService,
            _friendshipService,
            _reviewService,
            _gameListService);

        if (!userExists)
        {
            return NotFound();
        }

        // 2. Obtener los IDs de los juegos que el usuario ha "likeado"
        var likedGameIdsStrings = await _gameTrackingService.GetLikedGameIdsAsync(userId);
        
        // Convertir los IDs de string a Guid y filtrarlos si son inválidos
        var likedGameIds = likedGameIdsStrings
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        // 3. Obtener los detalles de los juegos "likeados" de forma concurrente
        var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
        foreach (var gameId in likedGameIds)
        {
            gamePreviewTasks.Add(_gameService.GetGamePreviewByIdAsync(gameId));
        }

        var gamePreviews = await Task.WhenAll(gamePreviewTasks);

        // 4. Filtrar los juegos que realmente se encontraron y asignarlos a la propiedad
        LikedGames = gamePreviews.Where(gp => gp != null).ToList()!; // El ! asegura que no es nulo si gp no es nulo
        
        return Page();
    }
}