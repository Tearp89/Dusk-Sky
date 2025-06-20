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
// using YourApp.ViewModels;

public class ProfileModel : ProfileModelBase
{
    // --- Servicios inyectados (mantener los existentes y añadir los necesarios) ---
    // (Ahora se asignan a los campos protegidos de ProfileModelBase)
    private readonly IUserManagerService _userManagerService_private; // Usar nombres diferentes para no confundir con los de la base
    private readonly IReviewService _reviewService_private;
    private readonly IGameListService _listService_private;
    private readonly IGameTrackingService _gameTrackingService_private; 
    private readonly IGameService _gameService_private; 
    private readonly IGameListItemService _gameListItemService_private;

    // --- Propiedades para las nuevas secciones ---
    public List<FriendViewModel> Friends { get; set; } = new(); 
    public QuickStatsViewModel QuickStats { get; set; } = new();
    public ActivityFeedItemViewModel? LatestActivity { get; set; }
    public List<GamePreviewDTO> RecentLikedGames { get; set; } = new(); 

    public ProfileModel(
        IAuthService authService, // Este servicio irá a ProfileModelBase._authService
        IUserManagerService userManagerService,
        IFriendshipService friendshipService, // Este servicio irá a ProfileModelBase._friendshipService
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService, 
        IGameService gameService,
        IGameListItemService gameListItemService) 
    {
        // Asignar los servicios a las propiedades PROTEGIDAS de ProfileModelBase
        _authService = authService; // Asignado a campo protegido en base
        _friendshipService = friendshipService; // Asignado a campo protegido en base

        // Asignar los demás servicios a los campos privados de esta clase (o si no se usan más que en OnGet, no se necesita campo privado)
        _userManagerService_private = userManagerService;
        _reviewService_private = reviewService;
        _listService_private = listService;
        _gameTrackingService_private = gameTrackingService;
        _gameService_private = gameService;
        _gameListItemService_private = gameListItemService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Profile";

        // Llamada a LoadProfileHeaderData que ahora usa los campos protegidos
        var userExists = await LoadProfileHeaderData(
            userId,
            _authService, // Se pasa el servicio, que se asigna a _authService en la base
            _userManagerService_private, // Se pasa el servicio
            _friendshipService, // Se pasa el servicio, que se asigna a _friendshipService en la base
            _reviewService_private,
            _listService_private,
            _gameTrackingService_private
        );

        if (!userExists) return NotFound();

        // --- Cargar Amigos ---
        // ¡OJO! _friendshipService ahora es un campo protegido, ya lo tienes accesible.
        var friendDocs = await _friendshipService.GetFriendsAsync(userId);
        if (friendDocs != null && friendDocs.Any())
        {
            var friendTasks = friendDocs.Select(async f =>
            {
                var friendId = f.SenderId == userId ? f.ReceiverId : f.SenderId;
                // ¡OJO! _authService ahora es un campo protegido
                var friendAuthUser = await _authService.SearchUserByIdAsync(friendId);
                var friendProfile = await _userManagerService_private.GetProfileAsync(friendId);
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

    // --- Métodos para cargar cada sección (asegúrate de que usen los servicios correctos) ---

    private async Task LoadQuickStats(string userId)
    {
        var completedGameIds = await _gameTrackingService_private.GetGameIdsByStatusAsync(userId, "played");
        QuickStats.CompletedGamesCount = completedGameIds?.Count ?? 0;
        QuickStats.ReviewsCount = ProfileHeader.ReviewCount; 
        var userReviews = await _reviewService_private.GetFriendsReviewsAsync(new List<string> {userId}); 
        if (userReviews != null && userReviews.Any())
        {
            QuickStats.AverageRating = Math.Round(userReviews.Average(r => r.Rating), 1); 
        } else { QuickStats.AverageRating = 0.0; }
        if (QuickStats.CompletedGamesCount > 20) QuickStats.PlayerRank = "Veteran";
        else if (QuickStats.CompletedGamesCount > 5) QuickStats.PlayerRank = "Explorer";
        else QuickStats.PlayerRank = "Newbie";
    }

    private async Task LoadLatestActivity(string userId)
    {
        var reviewsTask = _reviewService_private.GetFriendsReviewsAsync(new List<string> {userId}); 
        var gameTrackingsTask = _gameTrackingService_private.GetTrackingsByUserAsync(userId);
        var gameListsTask = _listService_private.GetUserListsAsync(userId);

        await Task.WhenAll(reviewsTask, gameTrackingsTask, gameListsTask);

        var allActivityItems = new List<ActivityFeedItemViewModel>();

        foreach (var r in reviewsTask.Result)
        {
            var gamePreview = await _gameService_private.GetGamePreviewByIdAsync(r.GameId);
            if (gamePreview != null) { /* ... mapeo ... */ }
        }
        foreach (var gt in gameTrackingsTask.Result)
        {
            var gamePreview = await _gameService_private.GetGamePreviewByIdAsync(Guid.Parse(gt.GameId));
            if (gamePreview != null) { /* ... mapeo ... */ }
        }
        var gameListActivityTasks = new List<Task<GameListActivityViewModel>>();
        foreach (var gl in gameListsTask.Result)
        {
            gameListActivityTasks.Add(Task.Run(async () => {
                var listItems = await _gameListItemService_private.GetItemsByListIdAsync(gl.Id);
                // ... mapeo ...
                return new GameListActivityViewModel { /* ... */ ItemCount = listItems?.Count ?? 0 };
            }));
        }
        allActivityItems.AddRange(await Task.WhenAll(gameListActivityTasks));
        LatestActivity = allActivityItems.OrderByDescending(a => a.Timestamp).FirstOrDefault();
    }

    private async Task LoadRecentLikedGames(string userId)
    {
        var likedGameIdsStrings = await _gameTrackingService_private.GetLikedGameIdsAsync(userId);
        var likedGameIds = likedGameIdsStrings.Where(id => Guid.TryParse(id, out _)).Select(Guid.Parse).ToList();
        var gamePreviewTasks = new List<Task<GamePreviewDTO?>>();
        foreach (var gameId in likedGameIds.Take(6)) { gamePreviewTasks.Add(_gameService_private.GetGamePreviewByIdAsync(gameId)); }
        RecentLikedGames = (await Task.WhenAll(gamePreviewTasks)).Where(gp => gp != null).ToList()!;
    }
    
    // --- Métodos OnPost para manejar las acciones de amistad ---
    // (Estos métodos no necesitan cambios adicionales, ya están en ProfileModel)

    [ValidateAntiForgeryToken] 
    public async Task<IActionResult> OnPostSendRequestAsync(string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid(); 

        if (loggedInUserId == profileUserId)
        {
            TempData["StatusMessage"] = "Error: No puedes enviarte una solicitud de amistad a ti mismo.";
            return RedirectToPage(new { userId = profileUserId });
        }

        var success = await _friendshipService.SendRequestAsync(loggedInUserId, profileUserId);
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad enviada.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo enviar la solicitud de amistad. Ya existe una pendiente o ya son amigos.";
        }
        return RedirectToPage(new { userId = profileUserId }); 
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAcceptRequestAsync(string requestId, string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid();

        var success = await _friendshipService.AcceptRequestAsync(requestId); 
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad aceptada. ¡Ahora son amigos!";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo aceptar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostRejectRequestAsync(string requestId, string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid();

        var success = await _friendshipService.RejectRequestAsync(requestId);
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad rechazada.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo rechazar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }
}