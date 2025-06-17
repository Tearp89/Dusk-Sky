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
    [BindProperty(SupportsGet = true)]
public string? Term { get; set; }


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
        await CargarDatosLista(id);
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        var id = RouteData.Values["id"]?.ToString() ?? "";
        await CargarDatosLista(id);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchTerm);

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

    public async Task<IActionResult> OnPostDeleteGameAsync(string listId, string itemId)
    {
        await _itemService.DeleteItemAsync(listId, itemId);
        return RedirectToPage(new { id = listId, SearchTerm });
    }

    public async Task<IActionResult> OnPostDeleteListAsync(string ListId)
{
    if (string.IsNullOrEmpty(ListId))
        return BadRequest();

    // Verifica si el usuario es dueño o tiene permisos
    var list = await _listService.GetListByIdAsync(ListId);
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

    bool isOwner = list.UserId == userId;
    bool isModOrAdmin = userRoles.Contains("moderator") || userRoles.Contains("admin");

    if (!isOwner && !isModOrAdmin)
        return Forbid();

    await _listService.DeleteListAsync(ListId);

    TempData["SuccessMessage"] = "La lista se eliminó correctamente.";
    return RedirectToPage("/Homepage/Index");
}






    public async Task<IActionResult> OnPostEditDescriptionAsync(string listId, string newDescription)
    {
        var list = await _listService.GetListByIdAsync(listId);
        if (list == null) return NotFound();

        list.Description = newDescription;
        await _listService.UpdateListAsync(list.Id, list);
        return RedirectToPage(new { id = listId, SearchTerm });
    }

    private async Task CargarDatosLista(string id)
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
                    Notes = item.Notes,
                    ItemId = item.Id
                });
            }
        }

        Games = combinedList;
        UserData = await _userService.GetProfileAsync(List.UserId);
    }
}
