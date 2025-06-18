using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels; // donde estén tus GameListPreviewViewModel, etc.

public class ListsProfileModel : ProfileModelBase
{
    // --- Servicios necesarios ---
    private readonly IGameListService _gameListService;
    private readonly IGameListItemService _gameListItemService; // <-- ¡Nuevo!
    private readonly IGameService _gameService; // <-- ¡Nuevo!
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IReviewService _reviewService;
    private readonly IGameTrackingService _gameTrackingService; 

    // --- Propiedad para guardar las listas de este usuario (ahora usa el nuevo ViewModel) ---
    public List<GameListPreviewViewModel> UserLists { get; set; } = new();

    public ListsProfileModel(
        IGameListService gameListService,
        IGameListItemService gameListItemService, // <-- Inyectar
        IGameService gameService,                 // <-- Inyectar
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameTrackingService gameTrackingService)
    {
        _gameListService = gameListService;
        _gameListItemService = gameListItemService; // <-- Asignar
        _gameService = gameService;                 // <-- Asignar
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _reviewService = reviewService;
        _gameTrackingService = gameTrackingService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Lists";

        var userExists = await LoadProfileHeaderData(
            userId,
            _authService,
            _userManagerService,
            _friendshipService,
            _reviewService,
            _gameListService
            /* , _gameTrackingService si lo usa ProfileModelBase */ ); 

        if (!userExists)
        {
            return NotFound();
        }

        var allLists = await _gameListService.GetUserListsAsync(userId);
        var filteredLists = new List<GameListDTO>();

        if (allLists != null && allLists.Any())
        {
            string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (loggedInUserId == userId)
            {
                filteredLists = allLists.OrderByDescending(l => l.CreatedAt).ToList();
            }
            else
            {
                filteredLists = allLists
                    .Where(l => l.IsPublic)
                    .OrderByDescending(l => l.CreatedAt)
                    .ToList();
            }
        }
        
        // --- Ahora, mapeamos a GameListPreviewViewModel y obtenemos las imágenes ---
        var listTasks = new List<Task<GameListPreviewViewModel>>();
        foreach (var listDto in filteredLists)
        {
            listTasks.Add(Task.Run(async () => {
                var previewViewModel = new GameListPreviewViewModel
                {
                    Id = listDto.Id,
                    UserId = listDto.UserId,
                    Name = listDto.Name,
                    Description = listDto.Description,
                    IsPublic = listDto.IsPublic,
                    CreatedAt = listDto.CreatedAt,
                    LikedBy = listDto.LikedBy
                };

                // Obtener los IDs de los ítems de la lista
                var listItems = await _gameListItemService.GetItemsByListIdAsync(listDto.Id);
                
                // Obtener las URLs de las imágenes de los juegos (tomamos un máximo de 3-4 para el preview)
                var gameImageTasks = new List<Task<GamePreviewDTO?>>();
                foreach (var item in listItems.Take(4)) // Tomar hasta 4 imágenes para la pila
                {
                    
                    {
                        gameImageTasks.Add(_gameService.GetGamePreviewByIdAsync(item.GameId)); 
                    }
                }
                var gamePreviews = await Task.WhenAll(gameImageTasks);
                previewViewModel.GameImageUrls = gamePreviews
                                                    .Where(gp => gp != null && !string.IsNullOrEmpty(gp.HeaderUrl))
                                                    .Select(gp => gp!.HeaderUrl) // Asegura que no es nulo
                                                    .ToList();

                return previewViewModel;
            }));
        }
        UserLists = (await Task.WhenAll(listTasks)).ToList();

        return Page();
    }
}