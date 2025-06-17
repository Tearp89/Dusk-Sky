using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Asumo que SearchListWithImagesDto está en un namespace que ya tienes referenciado
// using YourApp.DTOs;

public class ListsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IUserManagerService _userManagerService;
    private readonly IGameListItemService _listItemService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;

    // La propiedad pública ahora usa tu DTO
    public List<SearchListWithImagesDto> RecentLists { get; set; } = new();

    public ListsModel(
        IGameListService listService,
        IUserManagerService userManagerService,
        IGameListItemService listItemService,
        IGameService gameService,
        IAuthService authService)
    {
        _listService = listService;
        _userManagerService = userManagerService;
        _listItemService = listItemService;
        _gameService = gameService;
        _authService = authService;
    }

    public async Task OnGetAsync()
    {
        var recentListDTOs = await _listService.GetRecentListsAsync();

        var tasks = recentListDTOs.Select(async listDto =>
        {
            var userProfile = await _userManagerService.GetProfileAsync(listDto.UserId);
            var authUser = await _authService.SearchUserByIdAsync(listDto.UserId);

            var listItems = await _listItemService.GetItemsByListIdAsync(listDto.Id);
            var gameImageTasks = listItems.Take(5).Select(async item => { // Tomamos hasta 5 imágenes para el efecto
                var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                return game?.HeaderUrl;
            });
            var gameImageUrls = (await Task.WhenAll(gameImageTasks)).Where(url => !string.IsNullOrEmpty(url)).ToList();

            // Creamos una instancia de tu SearchListWithImagesDto
            return new SearchListWithImagesDto
            {
                Id = listDto.Id,
                Name = listDto.Name,
                Description = listDto.Description,
                UserId = listDto.UserId,
                UserName = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                AvatarUrl = userProfile?.AvatarUrl ?? "/images/default-avatar.png",
                GameHeaders = gameImageUrls 
            };
        });

        RecentLists = (await Task.WhenAll(tasks)).ToList();
    }
}