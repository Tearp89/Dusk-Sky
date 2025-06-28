using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// Asegúrate de que los using apunten a tus servicios y ViewModels
// using YourApp.Services;
// using YourApp.ViewModels;

// Asumo que tu ReviewDto que viene del servicio se ve algo así:



public class ReviewsProfileModel : ProfileModelBase // Hereda de la clase base
{
    // --- Servicios necesarios para esta página y para la clase base ---
    private readonly IReviewService _reviewService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IGameListService _listService;
    private readonly IGameService _gameService;
    private readonly IGameTrackingService _gameTrackingService;
    private readonly IModerationReportService _moderationService;

    // --- Propiedad para guardar las reseñas de este usuario ---
    public List<ReviewCardViewModel> UserReviews { get; set; } = new();

    public ReviewsProfileModel(
        IReviewService reviewService,
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IGameListService listService,
        IGameService gameService,
        IGameTrackingService gameTrackingService,
        IModerationReportService moderationReportService)
    {
        _reviewService = reviewService;
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _listService = listService;
        _gameService = gameService;
        _gameTrackingService = gameTrackingService;
        _moderationService = moderationReportService;
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        ActiveTab = "Reviews";

        var userExists = await LoadProfileHeaderData(
            userId,
            _authService,
            _userManagerService,
            _friendshipService,
            _reviewService,
            _listService,
            _gameTrackingService);

        if (!userExists)
        {
            return NotFound();
        }

        var reviewDocs = await _reviewService.GetFriendsReviewsAsync(new List<string> { userId });

        if (reviewDocs != null && reviewDocs.Any())
        {
            var reviewTasks = reviewDocs.Select(dto => MapToViewModelAsync(dto));
            UserReviews = (await Task.WhenAll(reviewTasks)).ToList();
        }

        return Page();
    }

    private async Task<ReviewCardViewModel> MapToViewModelAsync(ReviewDTO reviewDto)
    {
        var game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);

        return new ReviewCardViewModel
        {
            ReviewId = reviewDto.Id,
            GameId = reviewDto.GameId.ToString(),
            GameTitle = game?.Title ?? "Unknown Game",
            GameImageUrl = game?.HeaderUrl ?? "/images/noImage.png",
            UserId = reviewDto.UserId,
            UserName = this.ProfileHeader.Username,
            UserAvatarUrl = this.ProfileHeader.AvatarUrl,
            Content = reviewDto.Content,
            Rating = reviewDto.Rating,
            LikesCount = reviewDto.Likes,
            CreatedAt = reviewDto.CreatedAt
        };
    }
    
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostSendRequestAsync(string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid();

        if (loggedInUserId == profileUserId)
        {
            TempData["StatusMessage"] = "Error: No puedes enviarte una solicitud de amistad a ti mismo.";
            return RedirectToPage(new { userId = profileUserId });
        }

        var success = await _friendshipService.SendRequestAsync(loggedInUserId, profileUserId);
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad enviada.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo enviar la solicitud de amistad. Ya existe una pendiente o ya son amigos.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAcceptRequestAsync(string requestId, string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid();

        var success = await _friendshipService.AcceptRequestAsync(requestId);
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad aceptada. ¡Ahora son amigos!";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo aceptar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostRejectRequestAsync(string requestId, string profileUserId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(loggedInUserId)) return Forbid();

        var success = await _friendshipService.RejectRequestAsync(requestId);
        if (success)
        {
            TempData["StatusMessage"] = "Solicitud de amistad rechazada.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo rechazar la solicitud de amistad.";
        }
        return RedirectToPage(new { userId = profileUserId });
    }
    
    [ValidateAntiForgeryToken] // Siempre para formularios POST que modifican datos
    public async Task<IActionResult> OnPostReportUserAsync(string profileUserId, string reason)
    {
        // 1. Obtener el ID del usuario que está realizando el reporte
        string? reporterUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 2. Validar que el usuario esté autenticado
        if (string.IsNullOrEmpty(reporterUserId))
        {
            TempData["StatusMessage"] = "Error: Debes iniciar sesión para reportar un usuario.";
            return RedirectToPage(new { userId = profileUserId });
        }

        // 3. Validar que no se esté reportando a sí mismo
        if (reporterUserId == profileUserId)
        {
            TempData["StatusMessage"] = "Error: No puedes reportarte a ti mismo.";
            return RedirectToPage(new { userId = profileUserId });
        }

        // 4. Validar el motivo del reporte
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["StatusMessage"] = "Error: El motivo del reporte no puede estar vacío.";
            // Si quieres que el modal se mantenga abierto con el error, necesitarías un manejo más complejo de AJAX.
            // Por ahora, redirigimos para mostrar el TempData.
            return RedirectToPage(new { userId = profileUserId });
        }

        // 5. Crear el objeto ReportDTO con los datos del reporte
        var report = new ReportDTO
        {
            // Asume que Id se genera en el servicio o la base de datos, o aquí si es un GUID nuevo.
            // Para este ejemplo, lo generamos aquí si tu DTO no tiene un constructor predeterminado que lo haga.
            Id = Guid.NewGuid().ToString(), // Descomentar si tu ReportDTO no genera un ID automáticamente.
            ReportedUserId = profileUserId,   // El ID del usuario que está siendo reportado
            Reason = reason,
            ContentType = "User",              // Indica que este reporte es sobre un usuario
            Status = "pending",               // El estado inicial del reporte (a la espera de moderación)
            ReportedAt = DateTime.UtcNow       // Marca de tiempo de cuando se creó el reporte
        };

        // 6. Enviar el reporte a través de tu servicio de moderación
        bool success = await _moderationService.CreateAsync(report);

        // 7. Manejar el resultado y mostrar un mensaje al usuario
        if (success)
        {
            TempData["SuccessMessage"] = "El usuario ha sido reportado con éxito. Gracias por tu ayuda, investigaremos tu reporte.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: No se pudo enviar el reporte. Por favor, inténtalo de nuevo más tarde.";
        }

        // 8. Redirigir de nuevo a la página de perfil para mostrar el mensaje
        return RedirectToPage(new { userId = profileUserId });
    }
}