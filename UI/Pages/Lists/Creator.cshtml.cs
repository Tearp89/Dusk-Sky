using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

public class CreateListModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly ILogger<CreateListModel> _logger;

    public CreateListModel(IGameListService listService, ILogger<CreateListModel> logger)
    {
        _listService = listService ?? throw new ArgumentNullException(nameof(listService), "El servicio de lista de juegos no puede ser nulo.");
         _logger = logger ?? throw new ArgumentNullException(nameof(logger), "El logger no puede ser nulo.");
    }

    [BindProperty]
    public GameListDTO List { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public Guid? GameId { get; set; }

    [BindProperty]
    public string Tags { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
    {
        if (List == null)
        {
            TempData["ErrorMessage"] = "Los datos de la lista son inválidos.";
            _logger.LogWarning("Intento de OnPostAsync con un objeto List nulo.");
            return Page();
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Usuario no autenticado o ID de usuario no encontrado.";
                _logger.LogWarning("Intento de OnPostAsync sin un ID de usuario válido.");
                return Unauthorized();
            }

            List.UserId = userId;

            if (string.IsNullOrEmpty(List.UserId))
            {
                TempData["ErrorMessage"] = "El ID de usuario para la lista es nulo o vacío.";
                _logger.LogWarning("List.UserId es nulo o vacío antes de crear la lista."); 
                return Page();
            }

            var listId = await _listService.CreateListAsync(List);

            if (string.IsNullOrEmpty(listId))
            {
                TempData["ErrorMessage"] = "Hubo un problema al crear la lista. El servicio no devolvió un ID de lista válido.";
                _logger.LogError("El servicio CreateListAsync devolvió un ID de lista nulo o vacío para el usuario {UserId}.", userId); // ✅ Registro de error
                return Page();
            }
            _logger.LogInformation("Lista '{ListName}' (ID: {ListId}) creada exitosamente por el usuario {UserId}.", List.Name, listId, userId); // ✅ Registro de éxito

            return RedirectToPage("SelectGames", new { listId, gameId = GameId });
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError(ex, "ArgumentNullException en OnPostAsync al crear lista: {ErrorMessage}", ex.Message); 
            TempData["ErrorMessage"] = $"Error de validación: {ex.Message}";
            
            return Page();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "InvalidOperationException en OnPostAsync al crear lista: {ErrorMessage}", ex.Message); 
            TempData["ErrorMessage"] = $"Error de operación: {ex.Message}";
            
            return Page();
        } catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "HttpRequestException en OnPostAsync al comunicarse con el servicio: {ErrorMessage}", ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al crear la lista: {ex.Message}";
            return Page();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Ocurrió un error inesperado al crear la lista: {ex.Message}";
            return Page();
        }
    }
}