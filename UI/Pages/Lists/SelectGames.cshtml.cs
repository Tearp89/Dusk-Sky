using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

public class SelectGamesModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IGameListItemService _itemService;

    public SelectGamesModel(IGameService gameService, IGameListItemService itemService)
    {
        _gameService = gameService;
        _itemService = itemService;
    }

    [BindProperty(SupportsGet = true)]
    public string ListId { get; set; } = string.Empty;

    [BindProperty]
    public string SearchQuery { get; set; } = string.Empty;

    public List<GamePreviewDTO> SearchResults { get; set; } = new();

    public List<GamePreviewDTO> SelectedGames { get; set; } = new();

    private string SessionKey => $"SelectedGames_{ListId}";

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadSelectedGamesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchQuery);
        }

        await LoadSelectedGamesAsync();
        return Page();
    }

    public IActionResult OnPostAddGameAsync(string listId, Guid gameId)
    {
        var ids = GetSelectedGameIdsFromSession();
        if (ids.Contains(gameId))
        {
            TempData["ErrorMessage"] = "Este juego ya está en la lista.";
            return RedirectToPage(new { listId });
        }

        if (!ids.Contains(gameId))
        {
            ids.Add(gameId);
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ids));
        }

        return RedirectToPage(new { listId });
    }

    public IActionResult OnPostRemoveGameAsync(string listId, Guid gameId)
    {
        var ids = GetSelectedGameIdsFromSession();
        ids.RemoveAll(id => id == gameId);
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(ids));
        TempData["SuccessMessage"] = "Juego eliminado de la lista.";

        return RedirectToPage(new { listId });
    }

    public async Task<IActionResult> OnPostFinishAsync(string listId)
    {
        var ids = GetSelectedGameIdsFromSession();
        int order = 1;


        foreach (var gameId in ids)
        {
            bool exists = await _itemService.ExistsAsync(listId, gameId);
            if (!exists)
            {
                var item = new GameListItemDTO
                {
                    Id = Guid.NewGuid().ToString(),
                    ListId = listId,
                    GameId = gameId,
                    Order = order++
                };
                await _itemService.AddItemAsync(item);
            }
        }

        HttpContext.Session.Remove(SessionKey);
        TempData["SuccessMessage"] = "¡Lista guardada con juegos!";
        return RedirectToPage("/Homepage/Index");
    }



    private List<Guid> GetSelectedGameIdsFromSession()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(json) ?? new List<Guid>();
    }

    private async Task LoadSelectedGamesAsync()
    {
        var ids = GetSelectedGameIdsFromSession();
        foreach (var id in ids)
        {
            var game = await _gameService.GetGamePreviewByIdAsync(id);
            if (game != null && !SelectedGames.Any(g => g.Id == id))
            {
                SelectedGames.Add(game);
            }
        }
    }
}
