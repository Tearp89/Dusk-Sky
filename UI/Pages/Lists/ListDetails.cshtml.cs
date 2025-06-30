using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using

public class ListDetailsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IGameListItemService _itemService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ListDetailsModel> _logger; // ✅ Declaración del logger

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
        IGameService gameService,
        IAuthService authService,
        IModerationReportService moderationReportService,
        ILogger<ListDetailsModel> logger) // ✅ Inyección de ILogger
    {
        // Validaciones de nulos para los servicios inyectados en el constructor
        _listService = listService ?? throw new ArgumentNullException(nameof(listService), "GameListService no puede ser nulo.");
        _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService), "GameListItemService no puede ser nulo.");
        _userService = userService ?? throw new ArgumentNullException(nameof(userService), "UserManagerService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "GameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "AuthService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "ModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        try
        {
            // Validar que el ID de la lista no sea nulo o vacío
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("OnGetAsync: ID de lista no proporcionado."); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "ID de lista no proporcionado.";
                return RedirectToPage("/Error");
            }

            await LoadListData(id);
            _logger.LogInformation("OnGetAsync: Detalles de la lista '{ListId}' cargados exitosamente.", id); // ✅ Registro de información
            return Page();
        }
        catch (ArgumentException ex) // ✅ Catch específico para ArgumentException lanzado por LoadListData
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException lanzado por LoadListData
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con servicios externos
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Problema de conexión al cargar la lista: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción
        {
            _logger.LogError(ex, "OnGetAsync: Ocurrió un error inesperado al cargar los detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); // ✅ Registro de error
            TempData["ErrorMessage"] = $"Ocurrió un error inesperado al cargar los detalles de la lista: {ex.Message}";
            return RedirectToPage("/Error");
        }
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        var id = RouteData.Values["id"]?.ToString();

        // Validar que el ID de la ruta no sea nulo o vacío
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("OnPostSearchAsync: ID de lista no encontrado en RouteData para la búsqueda."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista no encontrado para la búsqueda.";
            return Page();
        }

        try
        {
            await LoadListData(id);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchTerm);
                SearchResults ??= new List<GamePreviewDTO>(); // Asegurar que no sea nulo
                _logger.LogInformation("OnPostSearchAsync: Búsqueda de juegos realizada con el término '{SearchTerm}' para la lista '{ListId}'.", SearchTerm, id); // ✅ Registro de información
            }
            else
            {
                _logger.LogInformation("OnPostSearchAsync: Término de búsqueda vacío para la lista '{ListId}'. No se realizó búsqueda de juegos.", id); // ✅ Registro de información
            }

            return Page();
        }
        catch (ArgumentException ex) // ✅ Catch específico para ArgumentException
        {
            _logger.LogError(ex, "OnPostSearchAsync: ArgumentException al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al buscar: {ex.Message}";
            return Page();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException
        {
            _logger.LogError(ex, "OnPostSearchAsync: InvalidOperationException al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al buscar: {ex.Message}";
            return Page();
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostSearchAsync: HttpRequestException al buscar juegos para la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al buscar juegos: {ex.Message}";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostSearchAsync: Ocurrió un error inesperado al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al realizar la búsqueda: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostReportListAsync(string listId, string reason)
    {
        // Validar que listId no sea nulo o vacío antes de cualquier operación
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostReportListAsync: ID de lista no proporcionado para reportar."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de la lista a reportar no proporcionado.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(listId);
            if (list == null)
            {
                _logger.LogWarning("OnPostReportListAsync: Lista '{ListId}' no encontrada para reportar.", listId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "La lista a reportar no fue encontrada.";
                return NotFound();
            }

            var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(reporterId))
            {
                _logger.LogWarning("OnPostReportListAsync: Usuario no autenticado intentando reportar lista '{ListId}'.", listId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "Debes iniciar sesión para reportar contenido.";
                return Forbid();
            }
            if (reporterId == list.UserId)
            {
                _logger.LogWarning("OnPostReportListAsync: Usuario '{ReporterId}' intentó reportar su propia lista '{ListId}'.", reporterId, listId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "No puedes reportar tu propio contenido.";
                return RedirectToPage(new { id = listId });
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogWarning("OnPostReportListAsync: Razón de reporte vacía para la lista '{ListId}' por el usuario '{ReporterId}'.", listId, reporterId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "La razón del reporte no puede estar vacía.";
                return RedirectToPage(new { id = listId });
            }

            // Validar que list.UserId no sea nulo antes de crear el DTO
            if (string.IsNullOrEmpty(list.UserId))
            {
                _logger.LogError("OnPostReportListAsync: El ID del propietario de la lista '{ListId}' es nulo.", listId); // ✅ Registro de error
                TempData["ErrorMessage"] = "El ID del propietario de la lista es nulo, no se puede reportar.";
                return RedirectToPage(new { id = listId });
            }

            var reportDto = new ReportDTO
            {
                Id = Guid.NewGuid().ToString(),
                ContentType = "GameList",
                ReportedUserId = list.UserId,
                Reason = reason,
                Status = "pending",
                ReportedAt = DateTime.UtcNow
            };

            await _moderationService.CreateAsync(reportDto);
            _logger.LogInformation("OnPostReportListAsync: Lista '{ListId}' reportada exitosamente por el usuario '{ReporterId}'.", listId, reporterId); // ✅ Registro de información

            TempData["SuccessMessage"] = "Tu reporte ha sido enviado correctamente.";
            return RedirectToPage(new { id = listId });
        }
        catch (ArgumentException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostReportListAsync: ArgumentException al reportar lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (InvalidOperationException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostReportListAsync: InvalidOperationException al reportar lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostReportListAsync: HttpRequestException al comunicarse con el servicio de moderación para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al reportar la lista: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostReportListAsync: Ocurrió un error inesperado al procesar el reporte de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al procesar el reporte: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
    }

    public async Task<IActionResult> OnPostAddGameAsync(Guid gameId, string? notes, string listId)
    {
        // Validar que listId no sea nulo o vacío
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de lista no proporcionado para añadir juego."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de la lista no proporcionado para añadir juego.";
            return BadRequest();
        }

        // Validar que gameId no sea Guid.Empty
        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de juego inválido ({GameId}) para la lista '{ListId}'.", gameId, listId); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de juego inválido.";
            return BadRequest();
        }

        try
        {
            List = await _listService.GetListByIdAsync(listId);

            if (List == null)
            {
                _logger.LogWarning("OnPostAddGameAsync: Lista '{ListId}' no encontrada al intentar añadir el juego '{GameId}'.", listId, gameId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "Lista no encontrada al intentar añadir el juego.";
                return NotFound();
            }

            bool alreadyExists = await _itemService.ExistsAsync(List.Id, gameId);
            if (alreadyExists)
            {
                _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' ya existe en la lista '{ListId}'.", gameId, listId); // ✅ Registro de información
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
            _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' añadido exitosamente a la lista '{ListId}'.", gameId, listId); // ✅ Registro de información
            TempData["SuccessMessage"] = "Juego añadido a la lista correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostAddGameAsync: ArgumentException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al añadir juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostAddGameAsync: InvalidOperationException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al añadir juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostAddGameAsync: HttpRequestException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al añadir el juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Ocurrió un error inesperado al añadir el juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al añadir el juego a la lista: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
    }

    public async Task<IActionResult> OnPostDeleteGameAsync(string listId, string itemId)
    {
        // Validar que listId e itemId no sean nulos o vacíos
        if (string.IsNullOrEmpty(listId) || string.IsNullOrEmpty(itemId))
        {
            _logger.LogWarning("OnPostDeleteGameAsync: ID de lista ('{ListId}') o ID de item ('{ItemId}') no proporcionado para eliminar juego.", listId, itemId); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de lista o ID de item no proporcionado para eliminar juego.";
            return BadRequest();
        }

        try
        {
            await _itemService.DeleteItemAsync(listId, itemId);
            _logger.LogInformation("OnPostDeleteGameAsync: Item '{ItemId}' eliminado exitosamente de la lista '{ListId}'.", itemId, listId); // ✅ Registro de información
            TempData["SuccessMessage"] = "Juego eliminado de la lista correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: ArgumentException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al eliminar juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: InvalidOperationException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al eliminar juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: HttpRequestException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al eliminar el juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: Ocurrió un error inesperado al eliminar el juego '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar el juego de la lista: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
    }

    public async Task<IActionResult> OnPostDeleteListAsync(string ListId)
    {
        if (string.IsNullOrEmpty(ListId))
        {
            _logger.LogWarning("OnPostDeleteListAsync: ID de la lista a eliminar no proporcionado."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de la lista a eliminar no proporcionado.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(ListId);
            if (list == null)
            {
                _logger.LogWarning("OnPostDeleteListAsync: Lista '{ListId}' no encontrada para eliminar.", ListId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "La lista a eliminar no fue encontrada.";
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Validar que userId no sea nulo antes de usarlo
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostDeleteListAsync: Usuario no autenticado intentando eliminar la lista '{ListId}'.", ListId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "Usuario no autenticado.";
                return Unauthorized();
            }

            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            bool isOwner = list.UserId == userId;
            bool isModOrAdmin = userRoles.Contains("moderator") || userRoles.Contains("admin");

            if (!isOwner && !isModOrAdmin)
            {
                _logger.LogWarning("OnPostDeleteListAsync: Usuario '{UserId}' sin permisos intentó eliminar la lista '{ListId}'.", userId, ListId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "No tienes permisos para eliminar esta lista.";
                return Forbid();
            }

            await _listService.DeleteListAsync(ListId);
            _logger.LogInformation("OnPostDeleteListAsync: Lista '{ListId}' eliminada exitosamente por el usuario '{UserId}'.", ListId, userId); // ✅ Registro de información

            TempData["SuccessMessage"] = "La lista se eliminó correctamente.";
            return RedirectToPage("/Homepage/Index");
        }
        catch (ArgumentException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: ArgumentException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al eliminar lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (InvalidOperationException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: InvalidOperationException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al eliminar lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: HttpRequestException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al eliminar la lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: Ocurrió un error inesperado al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar la lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
    }

    public async Task<IActionResult> OnPostEditDescriptionAsync(string listId, string newDescription)
    {
        // Validar que listId no sea nulo o vacío
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostEditDescriptionAsync: ID de la lista no proporcionado para editar la descripción."); // ✅ Registro de advertencia
            TempData["ErrorMessage"] = "ID de la lista no proporcionado para editar la descripción.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(listId);
            if (list == null)
            {
                _logger.LogWarning("OnPostEditDescriptionAsync: Lista '{ListId}' no encontrada para editar la descripción.", listId); // ✅ Registro de advertencia
                TempData["ErrorMessage"] = "Lista no encontrada para editar descripción.";
                return NotFound();
            }

            list.Description = newDescription ?? string.Empty;

            await _listService.UpdateListAsync(list.Id, list);
            _logger.LogInformation("OnPostEditDescriptionAsync: Descripción de la lista '{ListId}' actualizada exitosamente.", listId); // ✅ Registro de información
            TempData["SuccessMessage"] = "Descripción actualizada correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: ArgumentException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al editar descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) // ✅ Catch específico
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: InvalidOperationException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al editar descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: HttpRequestException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al editar la descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: Ocurrió un error inesperado al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al editar la descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
    }

    private async Task LoadListData(string id)
    {
        // Validar que el ID de la lista no sea nulo o vacío
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("LoadListData: ID de lista nulo o vacío proporcionado."); // ✅ Registro de advertencia
            throw new ArgumentException("El ID de la lista no puede ser nulo o vacío.", nameof(id));
        }

        List = await _listService.GetListByIdAsync(id);

        if (List == null)
        {
            _logger.LogWarning("LoadListData: Lista con ID '{ListId}' no encontrada.", id); // ✅ Registro de advertencia
            throw new InvalidOperationException($"La lista con ID '{id}' no fue encontrada.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwner = currentUserId == List.UserId;
        _logger.LogDebug("LoadListData: Usuario actual '{UserId}' es propietario de la lista '{ListId}': {IsOwner}.", currentUserId, id, IsOwner); // ✅ Registro de depuración

        var items = await _itemService.GetItemsByListIdAsync(id);
        var combinedList = new List<GamePreviewWithNotesDto>();

        // Validar que 'items' no sea nulo antes de iterar
        if (items != null)
        {
            foreach (var item in items.OrderBy(i => i.Order))
            {
                // Validar que item.GameId no sea Guid.Empty
                if (item == null || item.GameId == Guid.Empty) // ✅ Asegurar que item no sea nulo antes de GameId
                {
                    _logger.LogWarning("LoadListData: Item de lista inválido (ID: {ItemId}, GameId: {GameId}) encontrado en la lista '{ListId}'.", item?.Id, item?.GameId, id); // ✅ Registro de advertencia
                    continue; // Saltar items con GameId inválido o item nulo
                }

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
                else
                {
                    _logger.LogWarning("LoadListData: No se encontró la vista previa del juego para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, id); // ✅ Registro de advertencia
                }
            }
        }
        else
        {
            _logger.LogInformation("LoadListData: No se encontraron items para la lista '{ListId}'.", id); // ✅ Registro de información
        }

        Games = combinedList;

        // Validar que List.UserId no sea nulo antes de llamar a servicios
        if (string.IsNullOrEmpty(List.UserId))
        {
            _logger.LogError("LoadListData: El ID de usuario de la lista '{ListId}' es nulo o vacío.", id); // ✅ Registro de error
            throw new InvalidOperationException("El ID de usuario de la lista es nulo.");
        }

        UserProfileDTO? userProfile = await _userService.GetProfileAsync(List.UserId); // ✅ Usar nullable
        UserData = userProfile; // Asignar el resultado (podría ser nulo)

        // Validar que UserData no sea nulo después de obtener el perfil
        if (UserData == null)
        {
            _logger.LogError("LoadListData: No se pudo cargar el perfil de usuario para el ID: '{UserId}' de la lista '{ListId}'.", List.UserId, id); // ✅ Registro de error
            throw new InvalidOperationException($"No se pudo cargar el perfil de usuario para el ID: {List.UserId}");
        }

        UserSearchResultDto? usernameResult = await _authService.SearchUserByIdAsync(List.UserId); // ✅ Usar nullable

        // Validar que usernameResult no sea nulo y que tenga un Username
        if (usernameResult != null && !string.IsNullOrEmpty(usernameResult.Username))
        {
            UserData.Username = usernameResult.Username;
        }
        else
        {
            _logger.LogWarning("LoadListData: No se pudo obtener el nombre de usuario para el ID: '{UserId}' de la lista '{ListId}'. Estableciendo a 'Usuario Desconocido'.", List.UserId, id); // ✅ Registro de advertencia
            UserData.Username = "Usuario Desconocido"; // Establecer un valor predeterminado si el nombre de usuario no se puede obtener
        }
    }
}