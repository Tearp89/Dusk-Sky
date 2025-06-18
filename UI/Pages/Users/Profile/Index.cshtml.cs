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
// using YourApp.ViewModels; // donde estén tus QuickStatsViewModel, ActivityFeedItemViewModel, etc.

public class ProfileModel : ProfileModelBase
{
    // --- Servicios inyectados (mantener los existentes y añadir los necesarios) ---
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _listService;
    private readonly IGameTrackingService _gameTrackingService; // Necesario para juegos likeados y completados
    private readonly IGameService _gameService; // Necesario para detalles de juego
    private readonly IGameListItemService _gameListItemService;

    // --- Propiedades para las nuevas secciones ---
    public List<FriendViewModel> Friends { get; set; } = new(); // Mantener
    // public Dictionary<DateTime, int> DailyActivityCounts { get; set; } = new(); // Si lo quitaste, asegúrate de que no esté
    // public DateTime CalendarStartDate { get; set; } // Si lo quitaste, asegúrate de que no esté
    // public DateTime CalendarEndDate { get; set; } // Si lo quitaste, asegúrate de que no esté

    public QuickStatsViewModel QuickStats { get; set; } = new();
    public ActivityFeedItemViewModel? LatestActivity { get; set; }
    public List<GamePreviewDTO> RecentLikedGames { get; set; } = new(); // Reutilizamos GamePreviewDTO

    public ProfileModel(
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService, // Asegúrate de que esté inyectado
        IGameService gameService,
        IGameListItemService gameListItemService) // Asegúrate de que esté inyectado
    {
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _reviewService = reviewService;
        _listService = listService;
        _gameTrackingService = gameTrackingService;
        _gameService = gameService;
        _gameListItemService = gameListItemService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Profile"; 
        
        var userExists = await LoadProfileHeaderData(
            userId, 
            _authService, 
            _userManagerService, 
            _friendshipService,
            _reviewService,
            _listService);
            
        if (!userExists)
        {
            return NotFound();
        }

        // --- Cargar Amigos (ya lo tienes) ---
        var friendDocs = await _friendshipService.GetFriendsAsync(userId);
        if (friendDocs != null && friendDocs.Any())
        {
            var friendTasks = friendDocs.Select(async f =>
            {
                var friendId = f.SenderId == userId ? f.ReceiverId : f.SenderId;
                var friendAuthUser = await _authService.SearchUserByIdAsync(friendId);
                var friendProfile = await _userManagerService.GetProfileAsync(friendId);
                return new FriendViewModel
                {
                    UserId = friendId,
                    Username = friendAuthUser?.Username ?? "Unknown User",
                    AvatarUrl = friendProfile?.AvatarUrl ?? "/images/default_avatar.png"
                };
            });
            Friends = (await Task.WhenAll(friendTasks)).ToList();
        }

        // --- Cargar Datos para Quick Stats ---
        await LoadQuickStats(userId);

        // --- Cargar Datos para Latest Activity ---
        await LoadLatestActivity(userId);

        // --- Cargar Datos para Recent Liked Games ---
        await LoadRecentLikedGames(userId);

        return Page();
    }

    // --- Métodos para cargar cada sección ---

    private async Task LoadQuickStats(string userId)
    {
        // 1. Juegos Completados
        var completedGameIds = await _gameTrackingService.GetGameIdsByStatusAsync(userId, "completed"); // Asumo que tienes un "completed" status
        QuickStats.CompletedGamesCount = completedGameIds?.Count ?? 0;

        // 2. Reseñas
        QuickStats.ReviewsCount = ProfileHeader.ReviewCount; // Ya lo tenemos del ProfileHeader

        // 3. Promedio de Calificaciones (ejemplo)
        var userReviews = await _reviewService.GetFriendsReviewsAsync(new List<string> {userId});
        if (userReviews != null && userReviews.Any())
        {
            QuickStats.AverageRating = Math.Round(userReviews.Average(r => r.Rating), 1); // Redondea a 1 decimal
        }
        else
        {
            QuickStats.AverageRating = 0.0;
        }

        // 4. Player Rank (ejemplo, puedes definir tu propia lógica)
        // Podrías basarlo en el número de juegos completados o reseñas
        if (QuickStats.CompletedGamesCount > 20) QuickStats.PlayerRank = "Veteran";
        else if (QuickStats.CompletedGamesCount > 5) QuickStats.PlayerRank = "Explorer";
        else QuickStats.PlayerRank = "Newbie";
    }

    private async Task LoadLatestActivity(string userId)
    {
        var reviewsTask = _reviewService.GetFriendsReviewsAsync(new List<string> {userId});
        var gameTrackingsTask = _gameTrackingService.GetTrackingsByUserAsync(userId);
        var gameListsTask = _listService.GetUserListsAsync(userId);

        await Task.WhenAll(reviewsTask, gameTrackingsTask, gameListsTask);

        var allActivityItems = new List<ActivityFeedItemViewModel>();

        // Mapear reseñas (sin cambios)
        foreach (var r in reviewsTask.Result)
        {
            var gamePreview = await _gameService.GetGamePreviewByIdAsync(r.GameId);
            if (gamePreview != null)
            {
                allActivityItems.Add(new ReviewActivityViewModel
                {
                    Type = "Review", Timestamp = r.CreatedAt, UserId = r.UserId,
                    Username = ProfileHeader.Username, UserAvatarUrl = ProfileHeader.AvatarUrl,
                    ReviewId = r.Id, GameId = r.GameId.ToString(), GameTitle = gamePreview.Title,
                    GameImageUrl = gamePreview.HeaderUrl, Content = r.Content, Rating = r.Rating, LikesCount = r.Likes
                });
            }
        }
        
        // Mapear game trackings (sin cambios)
        foreach (var gt in gameTrackingsTask.Result)
        {
            var gamePreview = await _gameService.GetGamePreviewByIdAsync(Guid.Parse(gt.GameId));
            if (gamePreview != null)
            {
                allActivityItems.Add(new GameLogActivityViewModel
                {
                    Type = "GameLog", Timestamp = gt.LastUpdatedAt, UserId = gt.UserId, // Usar UpdatedAt
                    Username = ProfileHeader.Username, UserAvatarUrl = ProfileHeader.AvatarUrl,
                    GameTrackingId = gt.Id, GameId = gt.GameId, GameTitle = gamePreview.Title,
                    GameImageUrl = gamePreview.HeaderUrl, Status = gt.Status
                });
            }
        }
        
        // Mapear listas (¡CON CONTEO DE ÍTEMS AHORA!)
        // Procesamos las listas en tareas concurrentes para obtener el conteo de ítems
        var gameListActivityTasks = new List<Task<GameListActivityViewModel>>();
        foreach (var gl in gameListsTask.Result)
        {
            gameListActivityTasks.Add(Task.Run(async () => {
                var listItems = await _gameListItemService.GetItemsByListIdAsync(gl.Id);
                return new GameListActivityViewModel
                {
                    Type = "GameList", Timestamp = gl.CreatedAt, UserId = gl.UserId,
                    Username = ProfileHeader.Username, UserAvatarUrl = ProfileHeader.AvatarUrl,
                    ListId = gl.Id, ListName = gl.Name, Description = gl.Description,
                    ItemCount = listItems?.Count ?? 0 // <-- ¡Aquí se obtiene el conteo!
                };
            }));
        }
        allActivityItems.AddRange(await Task.WhenAll(gameListActivityTasks));


        // Obtener la actividad más reciente
        LatestActivity = allActivityItems.OrderByDescending(a => a.Timestamp).FirstOrDefault();
    }

    private async Task LoadRecentLikedGames(string userId)
    {
        var likedGameIdsStrings = await _gameTrackingService.GetLikedGameIdsAsync(userId);
        var likedGameIds = likedGameIdsStrings
            .Where(id => Guid.TryParse(id, out _))
            .Select(Guid.Parse)
            .ToList();

        var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
        // Tomar solo los últimos N juegos likeados, por ejemplo, 6
        foreach (var gameId in likedGameIds.Take(6)) // Muestra solo los 6 más recientes o los primeros 6 si no hay orden
        {
            gamePreviewTasks.Add(_gameService.GetGamePreviewByIdAsync(gameId));
        }

        RecentLikedGames = (await Task.WhenAll(gamePreviewTasks))
            .Where(gp => gp != null)
            .ToList()!;
        
        // Si necesitas ordenarlos por la fecha en que se le dio "like",
        // necesitarías que GetLikedGameIdsAsync devolviera un DTO con la fecha de "like".
        // Por ahora, se muestra el orden en que se recuperan los IDs.
    }
}