using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ListDetailsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IGameListItemService _itemService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;

    public GameListDTO List { get; set; } = null!;
    public List<GamePreviewWithNotesDto> Games { get; set; } = new();

    public UserProfileDTO UserData { get; set; } = null!;
    public bool IsOwner { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    public List<GamePreviewDTO> SearchResults { get; set; } = new();



    public ListDetailsModel(
        IGameListService listService,
        IGameListItemService itemService,
        IUserManagerService userService,
        IGameService gameService)
    {
        _listService = listService;
        _itemService = itemService;
        _userService = userService;
        _gameService = gameService;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        List = await _listService.GetListByIdAsync(id) ?? throw new Exception("Lista no encontrada");
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwner = currentUserId == List.UserId;


        var items = await _itemService.GetItemsByListIdAsync(id);
        var combinedList = new List<GamePreviewWithNotesDto>();

        foreach (var item in items.OrderBy(i => i.Order))
        {
            var preview = await _gameService.GetGamePreviewByIdAsync(item.GameId);
            if (preview != null)
            {
                combinedList.Add(new GamePreviewWithNotesDto
                {
                    Game = preview,
                    Notes = item.Notes
                });
            }
        }

        Games = combinedList;
        UserData = await _userService.GetProfileAsync(List.UserId);

        return Page();
    }

public async Task<IActionResult> OnPostAddGameAsync(Guid gameId, string? notes, string listId)
{
    List = await _listService.GetListByIdAsync(listId) ?? throw new Exception("Lista no encontrada");

    bool alreadyExists = await _itemService.ExistsAsync(List.Id, gameId);
    if (alreadyExists)
    {
        TempData["ErrorMessage"] = "El juego ya está en la lista.";
        return RedirectToPage(new { id = List.Id, SearchTerm });
    }

    var item = new GameListItemDTO
    {
        Id = Guid.NewGuid().ToString(),
        ListId = List.Id,
        GameId = gameId,
        Notes = notes ?? "",
        Order = 0
    };

    await _itemService.AddItemAsync(item);

    return RedirectToPage(new { id = listId, SearchTerm });

}



      public async Task<IActionResult> OnPostSearchAsync()
{
    // Cargar lista
    List = await _listService.GetListByIdAsync(RouteData.Values["id"]?.ToString() ?? "") ?? throw new Exception("Lista no encontrada");
    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    IsOwner = currentUserId == List.UserId;

    // Cargar juegos existentes
    var items = await _itemService.GetItemsByListIdAsync(List.Id);
    Games = new List<GamePreviewWithNotesDto>();
    foreach (var item in items.OrderBy(i => i.Order))
    {
        var preview = await _gameService.GetGamePreviewByIdAsync(item.GameId);
        if (preview != null)
        {
            Games.Add(new GamePreviewWithNotesDto
            {
                Game = preview,
                Notes = item.Notes
            });
        }
    }

    // Cargar datos del usuario dueño de la lista
    UserData = await _userService.GetProfileAsync(List.UserId);

    // Búsqueda
    if (!string.IsNullOrWhiteSpace(SearchTerm))
    {
        SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchTerm);
    }

    return Page();
}





}
