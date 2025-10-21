using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; 

public class ListDetailsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IGameListItemService _itemService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IModerationReportService _moderationService;
    private readonly ILogger<ListDetailsModel> _logger; 

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
        ILogger<ListDetailsModel> logger) 
    {
        _listService = listService ?? throw new ArgumentNullException(nameof(listService), "GameListService no puede ser nulo.");
        _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService), "GameListItemService no puede ser nulo.");
        _userService = userService ?? throw new ArgumentNullException(nameof(userService), "UserManagerService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "GameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "AuthService no puede ser nulo.");
        _moderationService = moderationReportService ?? throw new ArgumentNullException(nameof(moderationReportService), "ModerationReportService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        try
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("OnGetAsync: ID de lista no proporcionado."); 
                TempData["ErrorMessage"] = "ID de lista no proporcionado.";
                return RedirectToPage("/Error");
            }

            await LoadListData(id);
            _logger.LogInformation("OnGetAsync: Detalles de la lista '{ListId}' cargados exitosamente.", id); 
            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: ArgumentException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); 
            TempData["ErrorMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); 
            TempData["ErrorMessage"] = $"Operación inválida: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al cargar la lista: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Ocurrió un error inesperado al cargar los detalles de la lista '{ListId}'. Mensaje: {Message}", id, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error inesperado al cargar los detalles de la lista: {ex.Message}";
            return RedirectToPage("/Error");
        }
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        var id = RouteData.Values["id"]?.ToString();

        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("OnPostSearchAsync: ID de lista no encontrado en RouteData para la búsqueda."); 
            TempData["ErrorMessage"] = "ID de lista no encontrado para la búsqueda.";
            return Page();
        }

        try
        {
            await LoadListData(id);

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchTerm);
                SearchResults ??= new List<GamePreviewDTO>(); 
                _logger.LogInformation("OnPostSearchAsync: Búsqueda de juegos realizada con el término '{SearchTerm}' para la lista '{ListId}'.", SearchTerm, id); 
            }
            else
            {
                _logger.LogInformation("OnPostSearchAsync: Término de búsqueda vacío para la lista '{ListId}'. No se realizó búsqueda de juegos.", id); 
            }

            return Page();
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: ArgumentException al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al buscar: {ex.Message}";
            return Page();
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: InvalidOperationException al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al buscar: {ex.Message}";
            return Page();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: HttpRequestException al buscar juegos para la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al buscar juegos: {ex.Message}";
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostSearchAsync: Ocurrió un error inesperado al realizar la búsqueda en la lista '{ListId}'. Mensaje: {Message}", id, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al realizar la búsqueda: {ex.Message}";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostReportListAsync(string listId, string reason)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostReportListAsync: ID de lista no proporcionado para reportar."); 
            TempData["ErrorMessage"] = "ID de la lista a reportar no proporcionado.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(listId);
            if (list == null)
            {
                _logger.LogWarning("OnPostReportListAsync: Lista '{ListId}' no encontrada para reportar.", listId); 
                TempData["ErrorMessage"] = "La lista a reportar no fue encontrada.";
                return NotFound();
            }

            var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(reporterId))
            {
                _logger.LogWarning("OnPostReportListAsync: Usuario no autenticado intentando reportar lista '{ListId}'.", listId); 
                TempData["ErrorMessage"] = "Debes iniciar sesión para reportar contenido.";
                return Forbid();
            }
            if (reporterId == list.UserId)
            {
                _logger.LogWarning("OnPostReportListAsync: Usuario '{ReporterId}' intentó reportar su propia lista '{ListId}'.", reporterId, listId); 
                TempData["ErrorMessage"] = "No puedes reportar tu propio contenido.";
                return RedirectToPage(new { id = listId });
            }
            if (string.IsNullOrWhiteSpace(reason))
            {
                _logger.LogWarning("OnPostReportListAsync: Razón de reporte vacía para la lista '{ListId}' por el usuario '{ReporterId}'.", listId, reporterId); 
                TempData["ErrorMessage"] = "La razón del reporte no puede estar vacía.";
                return RedirectToPage(new { id = listId });
            }

            if (string.IsNullOrEmpty(list.UserId))
            {
                _logger.LogError("OnPostReportListAsync: El ID del propietario de la lista '{ListId}' es nulo.", listId); 
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
            _logger.LogInformation("OnPostReportListAsync: Lista '{ListId}' reportada exitosamente por el usuario '{ReporterId}'.", listId, reporterId); 

            TempData["SuccessMessage"] = "Tu reporte ha sido enviado correctamente.";
            return RedirectToPage(new { id = listId });
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostReportListAsync: ArgumentException al reportar lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostReportListAsync: InvalidOperationException al reportar lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (HttpRequestException ex)  
        {
            _logger.LogError(ex, "OnPostReportListAsync: HttpRequestException al comunicarse con el servicio de moderación para la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al reportar la lista: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostReportListAsync: Ocurrió un error inesperado al procesar el reporte de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al procesar el reporte: {ex.Message}";
            return RedirectToPage(new { id = listId });
        }
    }

    public async Task<IActionResult> OnPostAddGameAsync(Guid gameId, string? notes, string listId)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de lista no proporcionado para añadir juego."); 
            TempData["ErrorMessage"] = "ID de la lista no proporcionado para añadir juego.";
            return BadRequest();
        }

        if (gameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAddGameAsync: ID de juego inválido ({GameId}) para la lista '{ListId}'.", gameId, listId); 
            TempData["ErrorMessage"] = "ID de juego inválido.";
            return BadRequest();
        }

        try
        {
            List = await _listService.GetListByIdAsync(listId);

            if (List == null)
            {
                _logger.LogWarning("OnPostAddGameAsync: Lista '{ListId}' no encontrada al intentar añadir el juego '{GameId}'.", listId, gameId); 
                TempData["ErrorMessage"] = "Lista no encontrada al intentar añadir el juego.";
                return NotFound();
            }

            bool alreadyExists = await _itemService.ExistsAsync(List.Id, gameId);
            if (alreadyExists)
            {
                _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' ya existe en la lista '{ListId}'.", gameId, listId); 
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
            _logger.LogInformation("OnPostAddGameAsync: Juego '{GameId}' añadido exitosamente a la lista '{ListId}'.", gameId, listId); 
            TempData["SuccessMessage"] = "Juego añadido a la lista correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostAddGameAsync: ArgumentException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al añadir juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostAddGameAsync: InvalidOperationException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al añadir juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex)  
        {
            _logger.LogError(ex, "OnPostAddGameAsync: HttpRequestException al añadir juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al añadir el juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAddGameAsync: Ocurrió un error inesperado al añadir el juego '{GameId}' a la lista '{ListId}'. Mensaje: {Message}", gameId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al añadir el juego a la lista: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
    }

    public async Task<IActionResult> OnPostDeleteGameAsync(string listId, string itemId)
    {
        if (string.IsNullOrEmpty(listId) || string.IsNullOrEmpty(itemId))
        {
            _logger.LogWarning("OnPostDeleteGameAsync: ID de lista ('{ListId}') o ID de item ('{ItemId}') no proporcionado para eliminar juego.", listId, itemId); 
            TempData["ErrorMessage"] = "ID de lista o ID de item no proporcionado para eliminar juego.";
            return BadRequest();
        }

        try
        {
            await _itemService.DeleteItemAsync(listId, itemId);
            _logger.LogInformation("OnPostDeleteGameAsync: Item '{ItemId}' eliminado exitosamente de la lista '{ListId}'.", itemId, listId); 
            TempData["SuccessMessage"] = "Juego eliminado de la lista correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: ArgumentException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al eliminar juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: InvalidOperationException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al eliminar juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostDeleteGameAsync: HttpRequestException al eliminar item '{ItemId}' de la lista '{ListId}'. Mensaje: {Message}", itemId, listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al eliminar el juego: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) 
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
            _logger.LogWarning("OnPostDeleteListAsync: ID de la lista a eliminar no proporcionado."); 
            TempData["ErrorMessage"] = "ID de la lista a eliminar no proporcionado.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(ListId);
            if (list == null)
            {
                _logger.LogWarning("OnPostDeleteListAsync: Lista '{ListId}' no encontrada para eliminar.", ListId); 
                TempData["ErrorMessage"] = "La lista a eliminar no fue encontrada.";
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostDeleteListAsync: Usuario no autenticado intentando eliminar la lista '{ListId}'.", ListId); 
                TempData["ErrorMessage"] = "Usuario no autenticado.";
                return Unauthorized();
            }

            var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            bool isOwner = list.UserId == userId;
            bool isModOrAdmin = userRoles.Contains("moderator") || userRoles.Contains("admin");

            if (!isOwner && !isModOrAdmin)
            {
                _logger.LogWarning("OnPostDeleteListAsync: Usuario '{UserId}' sin permisos intentó eliminar la lista '{ListId}'.", userId, ListId); 
                TempData["ErrorMessage"] = "No tienes permisos para eliminar esta lista.";
                return Forbid();
            }

            await _listService.DeleteListAsync(ListId);
            _logger.LogInformation("OnPostDeleteListAsync: Lista '{ListId}' eliminada exitosamente por el usuario '{UserId}'.", ListId, userId); 

            TempData["SuccessMessage"] = "La lista se eliminó correctamente.";
            return RedirectToPage("/Homepage/Index");
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: ArgumentException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al eliminar lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: InvalidOperationException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al eliminar lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (HttpRequestException ex)  
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: HttpRequestException al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al eliminar la lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostDeleteListAsync: Ocurrió un error inesperado al eliminar la lista '{ListId}'. Mensaje: {Message}", ListId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al eliminar la lista: {ex.Message}";
            return RedirectToPage(new { id = ListId });
        }
    }

    public async Task<IActionResult> OnPostEditDescriptionAsync(string listId, string newDescription)
    {
        if (string.IsNullOrEmpty(listId))
        {
            _logger.LogWarning("OnPostEditDescriptionAsync: ID de la lista no proporcionado para editar la descripción."); 
            TempData["ErrorMessage"] = "ID de la lista no proporcionado para editar la descripción.";
            return BadRequest();
        }

        try
        {
            var list = await _listService.GetListByIdAsync(listId);
            if (list == null)
            {
                _logger.LogWarning("OnPostEditDescriptionAsync: Lista '{ListId}' no encontrada para editar la descripción.", listId); 
                TempData["ErrorMessage"] = "Lista no encontrada para editar descripción.";
                return NotFound();
            }

            list.Description = newDescription ?? string.Empty;

            await _listService.UpdateListAsync(list.Id, list);
            _logger.LogInformation("OnPostEditDescriptionAsync: Descripción de la lista '{ListId}' actualizada exitosamente.", listId); 
            TempData["SuccessMessage"] = "Descripción actualizada correctamente.";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: ArgumentException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de argumento al editar descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: InvalidOperationException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Error de operación al editar descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (HttpRequestException ex)  
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: HttpRequestException al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Problema de conexión al editar la descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostEditDescriptionAsync: Ocurrió un error inesperado al editar la descripción de la lista '{ListId}'. Mensaje: {Message}", listId, ex.Message);
            TempData["ErrorMessage"] = $"Ocurrió un error al editar la descripción: {ex.Message}";
            return RedirectToPage(new { id = listId, SearchTerm });
        }
    }

    private async Task LoadListData(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            _logger.LogWarning("LoadListData: ID de lista nulo o vacío proporcionado."); 
            throw new ArgumentException("El ID de la lista no puede ser nulo o vacío.", nameof(id));
        }

        List = await _listService.GetListByIdAsync(id);

        if (List == null)
        {
            _logger.LogWarning("LoadListData: Lista con ID '{ListId}' no encontrada.", id); 
            throw new InvalidOperationException($"La lista con ID '{id}' no fue encontrada.");
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwner = currentUserId == List.UserId;
        _logger.LogDebug("LoadListData: Usuario actual '{UserId}' es propietario de la lista '{ListId}': {IsOwner}.", currentUserId, id, IsOwner); 

        var items = await _itemService.GetItemsByListIdAsync(id);
        var combinedList = new List<GamePreviewWithNotesDto>();

        if (items != null)
        {
            foreach (var item in items.OrderBy(i => i.Order))
            {
                if (item == null || item.GameId == Guid.Empty)
                {
                    _logger.LogWarning("LoadListData: Item de lista inválido (ID: {ItemId}, GameId: {GameId}) encontrado en la lista '{ListId}'.", item?.Id, item?.GameId, id); 
                    continue; 
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
                    _logger.LogWarning("LoadListData: No se encontró la vista previa del juego para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, id); 
                }
            }
        }
        else
        {
            _logger.LogInformation("LoadListData: No se encontraron items para la lista '{ListId}'.", id); 
        }

        Games = combinedList;

        if (string.IsNullOrEmpty(List.UserId))
        {
            _logger.LogError("LoadListData: El ID de usuario de la lista '{ListId}' es nulo o vacío.", id); 
            throw new InvalidOperationException("El ID de usuario de la lista es nulo.");
        }

        UserProfileDTO? userProfile = await _userService.GetProfileAsync(List.UserId); 
        UserData = userProfile;

        if (UserData == null)
        {
            _logger.LogError("LoadListData: No se pudo cargar el perfil de usuario para el ID: '{UserId}' de la lista '{ListId}'.", List.UserId, id); 
            throw new InvalidOperationException($"No se pudo cargar el perfil de usuario para el ID: {List.UserId}");
        }

        UserSearchResultDto? usernameResult = await _authService.SearchUserByIdAsync(List.UserId); 

        if (usernameResult != null && !string.IsNullOrEmpty(usernameResult.Username))
        {
            UserData.Username = usernameResult.Username;
        }
        else
        {
            _logger.LogWarning("LoadListData: No se pudo obtener el nombre de usuario para el ID: '{UserId}' de la lista '{ListId}'. Estableciendo a 'Usuario Desconocido'.", List.UserId, id); 
            UserData.Username = "Usuario Desconocido"; 
        }
    }
}