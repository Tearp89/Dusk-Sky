using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using

[Authorize]
public class CreatorModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IReviewService _reviewService;
    private readonly IGameTrackingService _trackingService;
    private readonly IUserManagerService _userService;
    private readonly ILogger<CreatorModel> _logger; 

    public CreatorModel(
        IGameService gameService,
        IReviewService reviewService,
        IGameTrackingService trackingService,
        IUserManagerService userManager,
        ILogger<CreatorModel> logger) 
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _trackingService = trackingService ?? throw new ArgumentNullException(nameof(trackingService), "IGameTrackingService no puede ser nulo.");
        _userService = userManager ?? throw new ArgumentNullException(nameof(userManager), "IUserManagerService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    [BindProperty(SupportsGet = true)]
    public Guid GameId { get; set; }

    [BindProperty]
    public ReviewDTO Review { get; set; } = new();

    [BindProperty]
    public GameTrackingDto Tracking { get; set; } = new();

    [BindProperty]
    public bool Watched { get; set; }

    [BindProperty]
    public bool WatchedBefore { get; set; }

    [BindProperty]
    public DateTime? WatchedOn { get; set; }

    public GamePreviewDTO? Game { get; set; }
    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; } 

    public async Task<IActionResult> OnGetAsync()
    {
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnGetAsync: El ID del juego es Guid.Empty. Redirigiendo a /Error."); 
            TempData["ErrorMessage"] = "El ID del juego es requerido.";
            return RedirectToPage("/Error");
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Iniciando carga de detalles del juego '{GameId}'.", GameId); 
            Game = await _gameService.GetGamePreviewByIdAsync(GameId);

            if (Game == null)
            {
                _logger.LogWarning("OnGetAsync: Juego con ID '{GameId}' no encontrado.", GameId); 
                TempData["ErrorMessage"] = "Juego no encontrado.";
                return NotFound();
            }

            Review.GameId = GameId;
            Tracking.GameId = GameId.ToString();

            _logger.LogInformation("OnGetAsync: Detalles del juego '{GameId}' cargados exitosamente.", GameId); 
            return Page();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar el juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al cargar el juego: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: InvalidOperationException al cargar el juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Operación inválida al cargar el juego: {ex.Message}";
            return RedirectToPage("/Error");
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Ocurrió un error inesperado al cargar los detalles del juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error al cargar los detalles del juego: {ex.Message}";
            return RedirectToPage("/Error");
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (GameId == Guid.Empty)
        {
            _logger.LogWarning("OnPostAsync: El ID del juego es Guid.Empty. No se puede procesar la acción."); 
            TempData["ErrorMessage"] = "El ID del juego es requerido para procesar la acción.";
            return RedirectToPage("/Error");
        }

        try
        {
            _logger.LogInformation("OnPostAsync: Procesando reseña y seguimiento para el juego '{GameId}'.", GameId); 
            Game = await _gameService.GetGamePreviewByIdAsync(GameId);
            if (Game == null)
            {
                _logger.LogWarning("OnPostAsync: Juego con ID '{GameId}' no encontrado al intentar guardar la reseña y seguimiento.", GameId); 
                TempData["ErrorMessage"] = "Juego no encontrado al intentar guardar la reseña y seguimiento.";
                return NotFound();
            }

            if (Review == null)
            {
                ModelState.AddModelError("", "Datos de reseña inválidos.");
                _logger.LogWarning("OnPostAsync: Objeto Review es nulo en el momento del post."); 
                return Page();
            }

            var ratingStr = Request.Form["Review.Rating"];
            if (string.IsNullOrEmpty(ratingStr))
            {
                ModelState.AddModelError("Review.Rating", "La calificación es requerida.");
                _logger.LogWarning("OnPostAsync: Calificación de reseña vacía para el juego '{GameId}'.", GameId); 
            }
            else if (!double.TryParse(ratingStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedRating))
            {
                ModelState.AddModelError("Review.Rating", "Valor de calificación inválido.");
                _logger.LogWarning("OnPostAsync: Valor de calificación inválido '{RatingStr}' para el juego '{GameId}'.", ratingStr, GameId); 
            }
            else
            {
                Review.Rating = parsedRating;
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("OnPostAsync: Errores de validación de ModelState presentes para el juego '{GameId}'.", GameId); 
                return Page();
            }

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("OnPostAsync: Usuario no autenticado (userId es nulo o vacío) intentando crear reseña/seguimiento para el juego '{GameId}'.", GameId); 
                TempData["ErrorMessage"] = "Usuario no autenticado. Por favor, inicia sesión para realizar esta acción.";
                return Unauthorized();
            }

            Review.GameId = GameId;
            Review.CreatedAt = DateTime.UtcNow;
            Review.UserId = userId;
            Review.Likes = 0;
            Review.LikedBy = new List<string>();

            await _reviewService.CreateReviewAsync(Review);
            _logger.LogInformation("OnPostAsync: Reseña creada para el juego '{GameId}' por el usuario '{UserId}'.", GameId, userId); 

           
            if (Tracking == null)
            {
                ModelState.AddModelError("", "Datos de seguimiento inválidos.");
                _logger.LogWarning("OnPostAsync: Objeto Tracking es nulo en el momento del post."); 
                return Page();
            }

            Tracking.GameId = GameId.ToString();
            Tracking.UserId = userId;

            if (Watched || WatchedBefore)
            {
                Tracking.Status = "played";
            }
            else
            {
                Tracking.Status = "backlog";
            }

            await _trackingService.CreateAsync(Tracking);
            _logger.LogInformation("OnPostAsync: Seguimiento creado/actualizado para el juego '{GameId}' por el usuario '{UserId}'. Estado: {Status}, Gusta: {Liked}", GameId, userId, Tracking.Status, Tracking.Liked); 

            SuccessMessage = "Reseña y seguimiento guardados correctamente.";
            TempData["ErrorMessage"] = null; 
            return RedirectToPage("/Homepage/Index");
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostAsync: HttpRequestException al guardar reseña/seguimiento para el juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Problema de conexión al guardar la reseña: {ex.Message}";
            Game = await _gameService.GetGamePreviewByIdAsync(GameId); 
            return Page();
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostAsync: InvalidOperationException al guardar reseña/seguimiento para el juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Operación inválida al guardar la reseña: {ex.Message}";
            Game = await _gameService.GetGamePreviewByIdAsync(GameId);
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAsync: Ocurrió un error inesperado al guardar la reseña y el seguimiento para el juego '{GameId}'. Mensaje: {ErrorMessage}", GameId, ex.Message); 
            TempData["ErrorMessage"] = $"Ocurrió un error inesperado al guardar la reseña y el seguimiento: {ex.Message}";
            Game = await _gameService.GetGamePreviewByIdAsync(GameId);
            return Page();
        }
    }
}