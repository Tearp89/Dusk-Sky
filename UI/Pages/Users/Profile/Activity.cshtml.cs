using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels; // Donde están tus ActivityFeedItemViewModel, etc.

public class ActivityModel : ProfileModelBase
{
    // --- Servicios inyectados ---
    private readonly IReviewService _reviewService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IGameListService _gameListService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;


    // --- Propiedad para el feed de actividad ---
    public List<ActivityFeedItemViewModel> ActivityFeed { get; set; } = new();

    public ActivityModel(
        IReviewService reviewService,
        IGameTrackingService gameTrackingService,
        IGameListService gameListService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService)
    {
        _reviewService = reviewService;
        _gameTrackingService = gameTrackingService;
        _gameListService = gameListService;
        _gameService = gameService;
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Activity";

        // 1. Cargar los datos del encabezado del perfil (Avatar, Username, etc.)
        var userExists = await LoadProfileHeaderData(
            userId,
            _authService,
            _userManagerService,
            _friendshipService,
            _reviewService,
            _gameListService,
            _gameTrackingService);

        if (!userExists)
        {
            return NotFound();
        }

        // --- Tareas concurrentes para obtener todos los datos ---
        var reviewsTask = _reviewService.GetFriendsReviewsAsync(new List<string> {userId}); // Asumiendo que has añadido este método
        var gameTrackingsTask = _gameTrackingService.GetTrackingsByUserAsync(userId);
        var gameListsTask = _gameListService.GetUserListsAsync(userId);

        await Task.WhenAll(reviewsTask, gameTrackingsTask, gameListsTask);

        var reviews = reviewsTask.Result;
        var gameTrackings = gameTrackingsTask.Result;
        var gameLists = gameListsTask.Result;

        // --- Mapeo de Reseñas a ActivityFeedItemViewModel ---
        foreach (var r in reviews)
        {
            var gamePreview = await _gameService.GetGamePreviewByIdAsync(r.GameId); // r.GameId es Guid
            if (gamePreview != null) // Asegúrate de que el juego exista
            {
                ActivityFeed.Add(new ReviewActivityViewModel
                {
                    Type = "Review",
                    Timestamp = r.CreatedAt,
                    UserId = r.UserId,
                    Username = ProfileHeader.Username,
                    UserAvatarUrl = ProfileHeader.AvatarUrl,
                    ReviewId = r.Id,
                    GameId = r.GameId.ToString(),
                    GameTitle = gamePreview.Title,
                    GameImageUrl = gamePreview.HeaderUrl,
                    Content = r.Content,
                    Rating = r.Rating,
                    LikesCount = r.Likes
                });
            }
        }

        // --- Mapeo de Seguimientos de Juegos a ActivityFeedItemViewModel ---
        foreach (var gt in gameTrackings)
        {
            var gamePreview = await _gameService.GetGamePreviewByIdAsync(Guid.Parse(gt.GameId)); // gt.GameId es string, conviértelo
            if (gamePreview != null) // Asegúrate de que el juego exista
            {
                ActivityFeed.Add(new GameLogActivityViewModel
                {
                    Type = "GameLog",
                    // IMPORTANTE: Reemplaza con la propiedad de fecha real de GameTrackingDto
                    Timestamp = gt.LastUpdatedAt, // ASUMO QUE EXISTE ESTA PROPIEDAD
                    UserId = gt.UserId,
                    Username = ProfileHeader.Username,
                    UserAvatarUrl = ProfileHeader.AvatarUrl,
                    GameTrackingId = gt.Id,
                    GameId = gt.GameId, // Ya es string aquí
                    GameTitle = gamePreview.Title,
                    GameImageUrl = gamePreview.HeaderUrl,
                    Status = gt.Status
                });
            }
        }

        // --- Mapeo de Listas de Juegos a ActivityFeedItemViewModel ---
        foreach (var gl in gameLists)
        {
            ActivityFeed.Add(new GameListActivityViewModel
            {
                Type = "GameList",
                Timestamp = gl.CreatedAt,
                UserId = gl.UserId,
                Username = ProfileHeader.Username,
                UserAvatarUrl = ProfileHeader.AvatarUrl,
                ListId = gl.Id,
                ListName = gl.Name,
                Description = gl.Description,
                // Si quieres contar elementos de la lista, necesitarías GameListItemService.
                // Por ahora, asumiremos 0 si no hay una forma fácil de contarlos desde GameListDTO
                ItemCount = 0 // O gl.GameIds.Count si tu GameListDTO guarda los IDs de los juegos directamente
            });
        }

        // 4. Ordenar todas las actividades por fecha (más reciente primero)
        ActivityFeed = ActivityFeed.OrderByDescending(item => item.Timestamp).ToList();

        return Page();
    }
}