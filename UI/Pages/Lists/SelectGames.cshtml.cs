using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using

public class SelectGamesModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IGameListItemService _itemService;
    private readonly ILogger<SelectGamesModel> _logger; // ✅ Declaración del logger

    public SelectGamesModel(IGameService gameService, IGameListItemService itemService, ILogger<SelectGamesModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para los servicios y el logger inyectados en el constructor
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService), "IGameListItemService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
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
        // ✅ Validar que ListId no sea nulo o vacío antes de usarlo para la sesión
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnGetAsync: ID de lista no proporcionado. Redirigiendo a la página de inicio."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no proporcionado. No se pueden gestionar los juegos.";
            return RedirectToPage("/Homepage/Index");
        }

        try
        {
            var selected = GetSelectedGamesFromSession();

            // ✅ Validar que GameId.HasValue antes de intentar agregar
            if (GameId.HasValue && !selected.Any(s => s.GameId == GameId.Value))
            {
                selected.Add(new SelectedGameSessionItem
                {
                    GameId = GameId.Value,
                    Notes = string.Empty
                });
                SaveSelectedGamesToSession(selected);
                _logger.LogInformation("OnGetAsync: Juego '{GameId}' añadido desde la URL a la sesión para la lista '{ListId}'.", GameId.Value, ListId); // ✅ Registro de información
            }

            await LoadSelectedGamesAsync();
            _logger.LogInformation("OnGetAsync: Página cargada exitosamente para la lista '{ListId}'.", ListId); // ✅ Registro de información
            return Page();
        }
        catch (JsonException ex) // ✅ Catch específico para errores de JSON (deserialización de sesión)
        {
            _logger.LogError(ex, "OnGetAsync: Error de JSON al cargar juegos de la sesión para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Error de sesión: {ex.Message}. Intenta de nuevo.";
            return Page();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para operaciones inválidas
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar la página para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Error de operación al cargar la página: {ex.Message}";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnGetAsync: Ocurrió un error inesperado al cargar la página para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error al cargar la página: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        // ✅ Validar que ListId no sea nulo o vacío antes de usarlo
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnPostSearchAsync: ID de lista no proporcionado para la búsqueda. Redirigiendo a la página de inicio."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no proporcionado para la búsqueda.";
            return Page();
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchQuery);
                SearchResults ??= new List<GamePreviewDTO>(); // Asegurar que no sea nulo
                _logger.LogInformation("OnPostSearchAsync: Búsqueda de juegos realizada con el término '{SearchQuery}' para la lista '{ListId}'. Se encontraron {Count} resultados.", SearchQuery, ListId, SearchResults.Count); // ✅ Registro de información
            }
            else
            {
                _logger.LogInformation("OnPostSearchAsync: Término de búsqueda vacío para la lista '{ListId}'. No se realizó búsqueda de juegos.", ListId); // ✅ Registro de información
            }

            await LoadSelectedGamesAsync();
            return Page();
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con el servicio de juegos
        {
            _logger.LogError(ex, "OnPostSearchAsync: HttpRequestException al buscar juegos para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Problema de conexión al buscar juegos: {ex.Message}";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostSearchAsync: Ocurrió un error inesperado al realizar la búsqueda para la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error al realizar la búsqueda: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAddGameAsync(string listId, Guid gameId, string notes)
    {
        // ✅ Validar parámetros de entrada
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de lista no válido proporcionado."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no válido para añadir juego.";
            return BadRequest();
        }
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de juego no válido (Guid.Empty) proporcionado para la lista '{ListId}'.", listId); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de juego no válido.";
            return BadRequest();
        }

        try
        {
            ListId = listId; // ✅ Actualizar la propiedad ListId del modelo si viene del formulario

            var selected = GetSelectedGamesFromSession();

            if (selected.Any(g => g.GameId == gameId))
            {
                _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' ya existe en la selección de la lista '{ListId}'.", gameId, listId); // ✅ Registro de información
                TempData["ErrorMessage"] = "Este juego ya está en la lista seleccionada.";
                return RedirectToPage(new { listId });
            }

            selected.Add(new SelectedGameSessionItem
            {
                GameId = gameId,
                Notes = notes ?? string.Empty // ✅ Asegurar que Notes no sea nulo
            });

            SaveSelectedGamesToSession(selected);
            _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' añadido temporalmente a la sesión para la lista '{ListId}'.", gameId, listId); // ✅ Registro de información

            TempData["SuccessMessage"] = "Juego agregado temporalmente a la lista.";
            return RedirectToPage(new { listId });
        }
        catch (JsonException ex) // ✅ Catch específico para errores de JSON (serialización/deserialización de sesión)
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Error de JSON al manipular la sesión para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Error de sesión al añadir juego: {ex.Message}.";
            return RedirectToPage(new { listId });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Ocurrió un error inesperado al añadir el juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error al añadir el juego: {ex.Message}";
            return RedirectToPage(new { listId });
        }
    }

    public IActionResult OnPostRemoveGameAsync(string listId, Guid gameId)
    {
        // ✅ Validar parámetros de entrada
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostRemoveGameAsync: ID de lista no válido proporcionado."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no válido para eliminar juego.";
            return BadRequest();
        }
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostRemoveGameAsync: ID de juego no válido (Guid.Empty) proporcionado para la lista '{ListId}'.", listId); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de juego no válido para eliminar.";
            return BadRequest();
        }

        try
        {
            ListId = listId; // ✅ Actualizar la propiedad ListId del modelo si viene del formulario

            var selected = GetSelectedGamesFromSession();
            // ✅ Validar que 'selected' no sea nulo antes de operar
            selected?.RemoveAll(g => g.GameId == gameId);
            SaveSelectedGamesToSession(selected);
            _logger.LogInformation("OnPostRemoveGameAsync: Juego '{GameId}' eliminado de la sesión para la lista '{ListId}'.", gameId, listId); // ✅ Registro de información

            TempData["SuccessMessage"] = "Juego eliminado de la lista.";
            return RedirectToPage(new { listId });
        }
        catch (JsonException ex) // ✅ Catch específico para errores de JSON
        {
            _logger.LogError(ex, "OnPostRemoveGameAsync: Error de JSON al manipular la sesión para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Error de sesión al eliminar juego: {ex.Message}.";
            return RedirectToPage(new { listId });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostRemoveGameAsync: Ocurrió un error inesperado al eliminar el juego '{GameId}' de la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar el juego: {ex.Message}";
            return RedirectToPage(new { listId });
        }
    }

    public async Task<IActionResult> OnPostFinishAsync(string listId)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostFinishAsync: ID de lista no válido para finalizar el proceso."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no válido. No se puede finalizar el proceso.";
            return RedirectToPage("/Homepage/Index");
        }

        ListId = listId; // ✅ Actualiza el valor en la propiedad ListId (usado en SessionKey)

        try
        {
            var selected = GetSelectedGamesFromSession();

            // ✅ Validar que 'selected' no sea nulo antes de iterar
            if (selected == null || !selected.Any())
            {
                _logger.LogInformation("OnPostFinishAsync: No hay juegos seleccionados en la sesión para la lista '{ListId}'. Limpiando sesión.", listId); // ✅ Registro de información
                TempData["ErrorMessage"] = "No hay juegos seleccionados para guardar en la lista.";
                HttpContext.Session.Remove(SessionKey); // Limpiar sesión aunque no haya items
                return RedirectToPage("/Homepage/Index");
            }

            int order = 1;
            foreach (var item in selected)
            {
                // ✅ Validar que item no sea nulo y que GameId no sea Guid.Empty
                if (item == null || item.GameId == Guid.Empty)
                {
                    _logger.LogWarning("OnPostFinishAsync: Se encontró un item de sesión inválido (nulo o GameId vacío) para la lista '{ListId}'. Saltando.", listId); // ✅ Registro de advertencia
                    continue; // Salta items inválidos
                }

                bool exists = await _itemService.ExistsAsync(listId, item.GameId);
                if (!exists)
                {
                    var dto = new GameListItemDTO
                    {
                        Id = Guid.NewGuid().ToString(),
                        ListId = listId,
                        GameId = item.GameId,
                        Notes = item.Notes ?? string.Empty, // ✅ Asegurar que Notes no sea nulo
                        Order = order++
                    };
                    await _itemService.AddItemAsync(dto);
                    _logger.LogInformation("OnPostFinishAsync: Juego '{GameId}' añadido permanentemente a la lista '{ListId}'.", item.GameId, listId); // ✅ Registro de información
                }
                else
                {
                    _logger.LogInformation("OnPostFinishAsync: Juego '{GameId}' ya existe en la lista '{ListId}'. Saltando adición.", item.GameId, listId); // ✅ Registro de información
                }
            }

            HttpContext.Session.Remove(SessionKey);
            _logger.LogInformation("OnPostFinishAsync: Lista '{ListId}' guardada con juegos exitosamente. Sesión limpiada.", listId); // ✅ Registro de información
            TempData["SuccessMessage"] = "¡Lista guardada con juegos!";
            return RedirectToPage("/Homepage/Index");
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con el servicio de ítems
        {
            _logger.LogError(ex, "OnPostFinishAsync: HttpRequestException al guardar juegos en la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Problema de conexión al guardar la lista: {ex.Message}";
            return RedirectToPage("/Homepage/Index");
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostFinishAsync: Ocurrió un error inesperado al guardar la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error al guardar la lista: {ex.Message}";
            return RedirectToPage("/Homepage/Index");
        }
    }

    private async Task LoadSelectedGamesAsync()
    {
        // ✅ Validar que ListId no sea nulo o vacío antes de usarlo para la sesión
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("LoadSelectedGamesAsync: ID de lista nulo o vacío. No se cargarán juegos seleccionados."); // ✅ Registro de advertencia
            return;
        }

        var selected = GetSelectedGamesFromSession();
        SelectedGames.Clear();

        // ✅ Validar que 'selected' no sea nulo antes de iterar
        if (selected == null)
        {
            _logger.LogWarning("LoadSelectedGamesAsync: La lista de juegos seleccionados de la sesión es nula para la lista '{ListId}'.", ListId); // ✅ Registro de advertencia
            return;
        }

        foreach (var item in selected)
        {
            // ✅ Validar que item no sea nulo y que GameId no sea Guid.Empty
            if (item == null || item.GameId == Guid.Empty)
            {
                _logger.LogWarning("LoadSelectedGamesAsync: Se encontró un item de sesión inválido (nulo o GameId vacío) para la lista '{ListId}'. Saltando.", ListId); // ✅ Registro de advertencia
                continue; // Salta items inválidos
            }

            try
            {
                var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                if (game != null)
                {
                    SelectedGames.Add(new SelectedGamePreview
                    {
                        Game = game,
                        Notes = item.Notes ?? string.Empty // ✅ Asegurar que Notes no sea nulo
                    });
                }
                else
                {
                    _logger.LogWarning("LoadSelectedGamesAsync: No se encontró la vista previa del juego para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, ListId); // ✅ Registro de advertencia
                }
            }
            catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
            {
                _logger.LogError(ex, "LoadSelectedGamesAsync: HttpRequestException al obtener vista previa del juego '{GameId}' para la lista '{ListId}'. Mensaje: {Message}", item.GameId, ListId, ex.Message); // ✅ Registro de error
            }
            catch (Exception ex) // ✅ Catch general
            {
                _logger.LogError(ex, "LoadSelectedGamesAsync: Error inesperado al obtener vista previa del juego '{GameId}' para la lista '{ListId}'. Mensaje: {Message}", item.GameId, ListId, ex.Message); // ✅ Registro de error
            }
        }
        _logger.LogInformation("LoadSelectedGamesAsync: Juegos seleccionados cargados para la lista '{ListId}'. Total: {Count}.", ListId, SelectedGames.Count); // ✅ Registro de información
    }

    private List<SelectedGameSessionItem> GetSelectedGamesFromSession()
    {
        // ✅ Validar que HttpContext.Session no sea nulo
        if (HttpContext?.Session == null)
        {
            _logger.LogError("GetSelectedGamesFromSession: HttpContext.Session es nulo. Verifique la configuración de sesión."); // ✅ Registro de error
            return new List<SelectedGameSessionItem>();
        }

        var json = HttpContext.Session.GetString(SessionKey);
        
        try
        {
            // ✅ Usar el operador ?? para asegurar que el resultado de Deserialize no sea nulo
            var items = string.IsNullOrEmpty(json)
                ? new List<SelectedGameSessionItem>()
                : JsonSerializer.Deserialize<List<SelectedGameSessionItem>>(json);
            
            items ??= new List<SelectedGameSessionItem>(); // ✅ Asegurar que la deserialización no retorne nulo
            _logger.LogDebug("GetSelectedGamesFromSession: {Count} items cargados de la sesión para la clave '{SessionKey}'.", items.Count, SessionKey); // ✅ Registro de depuración
            return items;
        }
        catch (JsonException ex) // ✅ Catch específico para errores de deserialización
        {
            _logger.LogError(ex, "GetSelectedGamesFromSession: Error de deserialización JSON para la clave de sesión '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); // ✅ Registro de error
            // Podrías limpiar la sesión si el JSON es corrupto
            HttpContext.Session.Remove(SessionKey);
            return new List<SelectedGameSessionItem>();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "GetSelectedGamesFromSession: Error inesperado al obtener items de la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); // ✅ Registro de error
            return new List<SelectedGameSessionItem>();
        }
    }

    private void SaveSelectedGamesToSession(List<SelectedGameSessionItem> items)
    {
        // ✅ Validar que HttpContext.Session no sea nulo y que items no sea nulo
        if (HttpContext?.Session == null)
        {
            _logger.LogError("SaveSelectedGamesToSession: HttpContext.Session es nulo. No se pudo guardar en sesión."); // ✅ Registro de error
            return;
        }
        if (items == null)
        {
            _logger.LogWarning("SaveSelectedGamesToSession: Se intentó guardar una lista de items nula en la sesión para la clave '{SessionKey}'.", SessionKey); // ✅ Registro de advertencia
            return;
        }

        try
        {
            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(items));
            _logger.LogDebug("SaveSelectedGamesToSession: {Count} items guardados en la sesión para la clave '{SessionKey}'.", items.Count, SessionKey); // ✅ Registro de depuración
        }
        catch (JsonException ex) // ✅ Catch específico para errores de serialización
        {
            _logger.LogError(ex, "SaveSelectedGamesToSession: Error de serialización JSON al guardar en la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); // ✅ Registro de error
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "SaveSelectedGamesToSession: Error inesperado al guardar items en la sesión para la clave '{SessionKey}'. Mensaje: {Message}", SessionKey, ex.Message); // ✅ Registro de error
        }
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