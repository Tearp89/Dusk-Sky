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

    [BindProperty(SupportsGet = true)]
    public Guid? GameId { get; set; }

    public List<GamePreviewDTO> SearchResults { get; set; } = new();

    public List<SelectedGamePreview> SelectedGames { get; set; } = new();

    private string SessionKey => $"SelectedGames_{ListId}";

    public async Task<IActionResult> OnGetAsync()
    {
        var selected = GetSelectedGamesFromSession();

        if (GameId.HasValue && !selected.Any(s => s.GameId == GameId.Value))
        {
            selected.Add(new SelectedGameSessionItem
            {
                GameId = GameId.Value,
                Notes = string.Empty
            });
            SaveSelectedGamesToSession(selected);
        }

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

    public async Task<IActionResult> OnPostAddGameAsync(string listId, Guid gameId, string notes)
{
    var selected = GetSelectedGamesFromSession();

    if (selected.Any(g => g.GameId == gameId))
    {
        TempData["ErrorMessage"] = "Este juego ya está en la lista.";
        return RedirectToPage(new { listId });
    }

    // Agregar el nuevo juego con notas
    selected.Add(new SelectedGameSessionItem
    {
        GameId = gameId,
        Notes = notes
    });

    // Guardar en sesión
    SaveSelectedGamesToSession(selected);

    TempData["SuccessMessage"] = "Juego agregado temporalmente a la lista.";
    return RedirectToPage(new { listId });
}


    public IActionResult OnPostRemoveGameAsync(string listId, Guid gameId)
    {
        var selected = GetSelectedGamesFromSession();
        selected.RemoveAll(g => g.GameId == gameId);
        SaveSelectedGamesToSession(selected);

        TempData["SuccessMessage"] = "Juego eliminado de la lista.";
        return RedirectToPage(new { listId });
    }

    public async Task<IActionResult> OnPostFinishAsync(string listId)
{
    if (string.IsNullOrEmpty(listId))
    {
        TempData["ErrorMessage"] = "ID de lista no válido.";
        return RedirectToPage("/Homepage/Index");
    }

    // ✅ también actualiza el valor en la propiedad ListId (usado en SessionKey)
    ListId = listId;

    var selected = GetSelectedGamesFromSession();
    int order = 1;

    foreach (var item in selected)
    {
        bool exists = await _itemService.ExistsAsync(listId, item.GameId);
        if (!exists)
        {
            var dto = new GameListItemDTO
            {
                Id = Guid.NewGuid().ToString(),
                ListId = listId,
                GameId = item.GameId,
                Notes = item.Notes,
                Order = order++
            };
            await _itemService.AddItemAsync(dto);
        }
    }

    HttpContext.Session.Remove(SessionKey);
    TempData["SuccessMessage"] = "¡Lista guardada con juegos!";
    return RedirectToPage("/Homepage/Index");
}




    private async Task LoadSelectedGamesAsync()
    {
        var selected = GetSelectedGamesFromSession();
        SelectedGames.Clear();

        foreach (var item in selected)
        {
            var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
            if (game != null)
            {
                SelectedGames.Add(new SelectedGamePreview
                {
                    Game = game,
                    Notes = item.Notes
                });
            }
        }
    }

    private List<SelectedGameSessionItem> GetSelectedGamesFromSession()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(json)
            ? new List<SelectedGameSessionItem>()
            : JsonSerializer.Deserialize<List<SelectedGameSessionItem>>(json) ?? new();
    }

    private void SaveSelectedGamesToSession(List<SelectedGameSessionItem> items)
    {
        HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(items));
    }

    // Clase interna para guardar en sesión
    private class SelectedGameSessionItem
    {
        public Guid GameId { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    // Clase para renderizar en la interfaz
    public class SelectedGamePreview
    {
        public GamePreviewDTO Game { get; set; } = null!;
        public string Notes { get; set; } = string.Empty;
    }
}
