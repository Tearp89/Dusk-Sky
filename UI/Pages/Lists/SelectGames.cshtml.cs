using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using

public class SelectGamesModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IGameListItemService _itemService;
    private readonly ILogger<SelectGamesModel> _logger; 

    public SelectGamesModel(IGameService gameService, IGameListItemService itemService, ILogger<SelectGamesModel> logger) 
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService), "IGameListItemService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
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
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnGetAsync: ID de lista no proporcionado. Redirigiendo a la página de inicio."); 
            TempData["ErrorMessage"] = "ID de lista no proporcionado. No se pueden gestionar los juegos.";
            return RedirectToPage("/Homepage/Index");
        }

        try
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
                _logger.LogInformation("OnGetAsync: Juego '{GameId}' añadido desde la URL a la sesión para la lista '{ListId}'.", GameId.Value, ListId); 
            }

            await LoadSelectedGamesAsync();
            _logger.LogInformation("OnGetAsync: Página cargada exitosamente para la lista '{ListId}'.", ListId); 
            return Page();
        }
        catch (JsonException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Error de JSON al cargar juegos de la sesión para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); 
            TempData["ErrorMessage"] = $"Error de sesión: {ex.Message}. Intenta de nuevo.";
            return Page();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para operaciones inválidas
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar la página para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); 
            TempData["ErrorMessage"] = $"Error de operación al cargar la página: {ex.Message}";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnGetAsync: Ocurrió un error inesperado al cargar la página para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al cargar la página: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnPostSearchAsync: ID de lista no proporcionado para la búsqueda. Redirigiendo a la página de inicio."); 
            TempData["ErrorMessage"] = "ID de lista no proporcionado para la búsqueda.";
            return Page();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchQuery);
                SearchResults ??= new List<GamePreviewDTO>(); 
                _logger.LogInformation("OnPostSearchAsync: Búsqueda de juegos realizada con el término '{SearchQuery}' para la lista '{ListId}'. Se encontraron {Count} resultados.", SearchQuery, ListId, SearchResults.Count); 
            }
            else
            {
                _logger.LogInformation("OnPostSearchAsync: Término de búsqueda vacío para la lista '{ListId}'. No se realizó búsqueda de juegos.", ListId); 
            }

            await LoadSelectedGamesAsync();
            return Page();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: HttpRequestException al buscar juegos para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al buscar juegos: {ex.Message}";
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: Ocurrió un error inesperado al realizar la búsqueda para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al realizar la búsqueda: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddGameAsync(string listId, Guid gameId, string notes)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de lista no válido proporcionado."); 
            TempData["ErrorMessage"] = "ID de lista no válido para añadir juego.";
            return BadRequest();
        }
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de juego no válido (Guid.Empty) proporcionado para la lista '{ListId}'.", listId); 
            TempData["ErrorMessage"] = "ID de juego no válido.";
            return BadRequest();
        }

        try
        {
            ListId = listId; 

            var selected = GetSelectedGamesFromSession();

            if (selected.Any(g => g.GameId == gameId))
            {
                _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' ya existe en la selección de la lista '{ListId}'.", gameId, listId); 
                TempData["ErrorMessage"] = "Este juego ya está en la lista seleccionada.";
                return RedirectToPage(new { listId });
            }

            selected.Add(new SelectedGameSessionItem
            {
                GameId = gameId,
                Notes = notes ?? string.Empty 
            });

            SaveSelectedGamesToSession(selected);
            _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' añadido temporalmente a la sesión para la lista '{ListId}'.", gameId, listId); 

            TempData["SuccessMessage"] = "Juego agregado temporalmente a la lista.";
            return RedirectToPage(new { listId });
        }
        catch (JsonException ex) 
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Error de JSON al manipular la sesión para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); 
            TempData["ErrorMessage"] = $"Error de sesión al añadir juego: {ex.Message}.";
            return RedirectToPage(new { listId });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Ocurrió un error inesperado al añadir el juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al añadir el juego: {ex.Message}";
            return RedirectToPage(new { listId });
        }
    }

    public IActionResult OnPostRemoveGameAsync(string listId, Guid gameId)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostRemoveGameAsync: ID de lista no válido proporcionado."); 
            TempData["ErrorMessage"] = "ID de lista no válido para eliminar juego.";
            return BadRequest();
        }
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostRemoveGameAsync: ID de juego no válido (Guid.Empty) proporcionado para la lista '{ListId}'.", listId); 
            TempData["ErrorMessage"] = "ID de juego no válido para eliminar.";
            return BadRequest();
        }

        try
        {
            ListId = listId; 

            var selected = GetSelectedGamesFromSession();
            selected?.RemoveAll(g => g.GameId == gameId);
            SaveSelectedGamesToSession(selected);
            _logger.LogInformation("OnPostRemoveGameAsync: Juego '{GameId}' eliminado de la sesión para la lista '{ListId}'.", gameId, listId); 

            TempData["SuccessMessage"] = "Juego eliminado de la lista.";
            return RedirectToPage(new { listId });
        }
        catch (JsonException ex) 
        {
            _logger.LogError(ex, "OnPostRemoveGameAsync: Error de JSON al manipular la sesión para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); 
            TempData["ErrorMessage"] = $"Error de sesión al eliminar juego: {ex.Message}.";
            return RedirectToPage(new { listId });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostRemoveGameAsync: Ocurrió un error inesperado al eliminar el juego '{GameId}' de la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar el juego: {ex.Message}";
            return RedirectToPage(new { listId });
        }
    }

    public async Task<IActionResult> OnPostFinishAsync(string listId)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostFinishAsync: ID de lista no válido para finalizar el proceso."); 
            TempData["ErrorMessage"] = "ID de lista no válido. No se puede finalizar el proceso.";
            return RedirectToPage("/Homepage/Index");
        }

        ListId = listId; 

        try
        {
            var selected = GetSelectedGamesFromSession();

            if (selected == null || !selected.Any())
            {
                _logger.LogInformation("OnPostFinishAsync: No hay juegos seleccionados en la sesión para la lista '{ListId}'. Limpiando sesión.", listId); 
                TempData["ErrorMessage"] = "No hay juegos seleccionados para guardar en la lista.";
                HttpContext.Session.Remove(SessionKey); 
                return RedirectToPage("/Homepage/Index");
            }

            int order = 1;
            foreach (var item in selected)
            {
                if (item == null || item.GameId == Guid.Empty)
                {
                    _logger.LogWarning("OnPostFinishAsync: Se encontró un item de sesión inválido (nulo o GameId vacío) para la lista '{ListId}'. Saltando.", listId); 
                    continue; 
                }

                bool exists = await _itemService.ExistsAsync(listId, item.GameId);
                if (!exists)
                {
                    var dto = new GameListItemDTO
                    {
                        Id = Guid.NewGuid().ToString(),
                        ListId = listId,
                        GameId = item.GameId,
                        Notes = item.Notes ?? string.Empty, 
                        Order = order++
                    };
                    await _itemService.AddItemAsync(dto);
                    _logger.LogInformation("OnPostFinishAsync: Juego '{GameId}' añadido permanentemente a la lista '{ListId}'.", item.GameId, listId); 
                }
                else
                {
                    _logger.LogInformation("OnPostFinishAsync: Juego '{GameId}' ya existe en la lista '{ListId}'. Saltando adición.", item.GameId, listId); 
                }
            }

            HttpContext.Session.Remove(SessionKey);
            _logger.LogInformation("OnPostFinishAsync: Lista '{ListId}' guardada con juegos exitosamente. Sesión limpiada.", listId); 
            TempData["SuccessMessage"] = "¡Lista guardada con juegos!";
            return RedirectToPage("/Homepage/Index");
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostFinishAsync: HttpRequestException al guardar juegos en la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al guardar la lista: {ex.Message}";
            return RedirectToPage("/Homepage/Index");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostFinishAsync: Ocurrió un error inesperado al guardar la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al guardar la lista: {ex.Message}";
            return RedirectToPage("/Homepage/Index");
        }
    }

    private async Task LoadSelectedGamesAsync()
    {
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("LoadSelectedGamesAsync: ID de lista nulo o vacío. No se cargarán juegos seleccionados."); 
            return;
        }

        var selected = GetSelectedGamesFromSession();
        SelectedGames.Clear();

        if (selected == null)
        {
            _logger.LogWarning("LoadSelectedGamesAsync: La lista de juegos seleccionados de la sesión es nula para la lista '{ListId}'.", ListId); 
            return;
        }

        foreach (var item in selected)
        {
            if (item == null || item.GameId == Guid.Empty)
            {
                _logger.LogWarning("LoadSelectedGamesAsync: Se encontró un item de sesión inválido (nulo o GameId vacío) para la lista '{ListId}'. Saltando.", ListId); 
                continue; 
            }

            try
            {
                var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                if (game != null)
                {
                    SelectedGames.Add(new SelectedGamePreview
                    {
                        Game = game,
                        Notes = item.Notes ?? string.Empty 
                    });
                }
                else
                {
                    _logger.LogWarning("LoadSelectedGamesAsync: No se encontró la vista previa del juego para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, ListId); 
                }
            }
            catch (HttpRequestException ex) 
            {
                _logger.LogError(ex, "LoadSelectedGamesAsync: HttpRequestException al obtener vista previa del juego '{GameId}' para la lista '{ListId}'. Mensaje: {Message}", item.GameId, ListId, ex.Message); 
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "LoadSelectedGamesAsync: Error inesperado al obtener vista previa del juego '{GameId}' para la lista '{ListId}'. Mensaje: {Message}", item.GameId, ListId, ex.Message); 
            }
        }
        _logger.LogInformation("LoadSelectedGamesAsync: Juegos seleccionados cargados para la lista '{ListId}'. Total: {Count}.", ListId, SelectedGames.Count); 
    }

    private List<SelectedGameSessionItem> GetSelectedGamesFromSession()
    {
        if (HttpContext?.Session == null)
        {
            _logger.LogError("GetSelectedGamesFromSession: HttpContext.Session es nulo. Verifique la configuración de sesión."); 
            return new List<SelectedGameSessionItem>();
        }

        var json = HttpContext.Session.GetString(SessionKey);
        
        try
        {
            var items = string.IsNullOrEmpty(json)
                ? new List<SelectedGameSessionItem>()
                : JsonSerializer.Deserialize<List<SelectedGameSessionItem>>(json);
            
            items ??= new List<SelectedGameSessionItem>(); 
            _logger.LogDebug("GetSelectedGamesFromSession: {Count} items cargados de la sesión para la clave '{SessionKey}'.", items.Count, SessionKey); 
            return items;
        }
        catch (JsonException ex) 
        {
            _logger.LogError(ex, "GetSelectedGamesFromSession: Error de deserialización JSON para la clave de sesión '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); 
            HttpContext.Session.Remove(SessionKey);
            return new List<SelectedGameSessionItem>();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "GetSelectedGamesFromSession: Error inesperado al obtener items de la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); 
            return new List<SelectedGameSessionItem>();
        }
    }

    private void SaveSelectedGamesToSession(List<SelectedGameSessionItem> items)
    {
        if (HttpContext?.Session == null)
        {
            _logger.LogError("SaveSelectedGamesToSession: HttpContext.Session es nulo. No se pudo guardar en sesión."); 
            return;
        }
        if (items == null)
        {
            _logger.LogWarning("SaveSelectedGamesToSession: Se intentó guardar una lista de items nula en la sesión para la clave '{SessionKey}'.", SessionKey); 
            return;
        }

        try
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(items));
            _logger.LogDebug("SaveSelectedGamesToSession: {Count} items guardados en la sesión para la clave '{SessionKey}'.", items.Count, SessionKey); 
        }
        catch (JsonException ex) 
        {
            _logger.LogError(ex, "SaveSelectedGamesToSession: Error de serialización JSON al guardar en la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); 
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "SaveSelectedGamesToSession: Error inesperado al guardar items en la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); 
        }
    }

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