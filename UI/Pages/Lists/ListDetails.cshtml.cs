using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ListDetailsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IGameListItemService _itemService;
    private readonly IUserManagerService _userService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IModerationReportService _moderationService;

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
        IModerationReportService moderationReportService)
    {
        _listService = listService;
        _itemService = itemService;
        _userService = userService;
        _gameService = gameService;
        _authService = authService;
        _moderationService = moderationReportService;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await LoadListData(id);
        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync()
    {
        var id = RouteData.Values["id"]?.ToString() ?? "";
        await LoadListData(id);

        if (!string.IsNullOrWhiteSpace(SearchTerm))
            SearchResults = await _gameService.SearchGamePreviewsByNameAsync(SearchTerm);

        return Page();
    }

    // En ListDetailsModel.cs
// La firma del método ahora es más simple, solo recibe lo que viene del formulario.
public async Task<IActionResult> OnPostReportListAsync(string listId, string reason)
{
    // 1. Cargar la lista desde la base de datos para obtener todos sus datos.
    //    Esto soluciona el error de NullReferenceException.
    var list = await _listService.GetListByIdAsync(listId);
    if (list == null)
    {
        return NotFound(); // Si la lista no existe, no se puede reportar.
    }

    // 2. Obtener el ID del usuario que está haciendo el reporte.
    var reporterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    // 3. Validaciones de seguridad.
    if (string.IsNullOrEmpty(reporterId))
    {
        return Forbid(); // El usuario debe estar logueado.
    }
    if (reporterId == list.UserId)
    {
        TempData["ErrorMessage"] = "You cannot report your own content.";
        return RedirectToPage(new { id = listId });
    }
    if (string.IsNullOrWhiteSpace(reason))
    {
        TempData["ErrorMessage"] = "The reason for the report cannot be empty.";
        return RedirectToPage(new { id = listId });
    }

    // 4. Crear el objeto DTO para el reporte de forma segura.
    //    Usamos el nombre de tu servicio inyectado: _moderationService
    //    Y el DTO que espera ese servicio (probablemente ModerationReportDTO).
    var reportDto = new ReportDTO
    {
         Id = Guid.NewGuid().ToString(),
        ContentType = "GameList", // Se establece en el servidor, es más seguro.
        ReportedUserId = list.UserId, // Obtenido de la lista que cargamos.
        Reason = reason,
        Status = "pending",
        ReportedAt = DateTime.UtcNow // Se genera en el servidor, es más fiable.
    };

    // 5. Llamar al servicio para guardar el reporte.
    //    Usa el nombre correcto de tu variable de servicio (_moderationService)
    await _moderationService.CreateAsync(reportDto);

    // 6. Enviar mensaje de éxito y redirigir.
    TempData["SuccessMessage"] = "Your report has been submitted successfully.";
    return RedirectToPage(new { id = listId });
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

    private async Task LoadListData(string id)
    {
        List = await _listService.GetListByIdAsync(id) ?? throw new Exception("List not found");
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
        var username = await _authService.SearchUserByIdAsync(List.UserId);
        UserData.Username = username.Username;
    }
}
