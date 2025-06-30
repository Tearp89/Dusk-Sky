using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Necesitas este using para ILogger

public class ReviewsModel : PageModel
{
    private readonly IReviewService _reviewService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly ILogger<ReviewsModel> _logger; // ✅ Inyectamos el logger

    public List<ReviewCardViewModel> PopularReviews { get; set; } = new();
    public List<ReviewCardViewModel> RecentReviews { get; set; } = new();

    public ReviewsModel(
        IReviewService reviewService,
        IGameService gameService,
        IAuthService authService,
        IUserManagerService userManagerService,
        ILogger<ReviewsModel> logger) // ✅ Agregamos ILogger al constructor
    {
        // ✅ Validaciones de nulos para servicios y logger
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService), "IReviewService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
    }

    public async Task OnGetAsync()
    {
        try
        {
            var popularReviewDtos = await _reviewService.GetTopReviewsAsync();
            var recentReviewDtos = await _reviewService.GetRecentReviewsAsync();

            popularReviewDtos ??= new List<ReviewDTO>();
            recentReviewDtos ??= new List<ReviewDTO>();

            var popularTasks = popularReviewDtos.Select(dto => MapToViewModelAsync(dto));
            var recentTasks = recentReviewDtos.Select(dto => MapToViewModelAsync(dto));

            PopularReviews = (await Task.WhenAll(popularTasks)).ToList();
            RecentReviews = (await Task.WhenAll(recentTasks)).ToList();
        }
        catch (ArgumentNullException ex) // ✅ Catch específico para argumentos nulos (ej. si un servicio devuelve inesperadamente una colección nula)
        {
            _logger.LogError(ex, "Error de argumento nulo al cargar las reseñas. Detalles: {Message}", ex.Message);
            TempData["ErrorMessage"] = "Hubo un problema de datos al cargar las reseñas. Por favor, inténtalo de nuevo más tarde.";
            PopularReviews = new List<ReviewCardViewModel>();
            RecentReviews = new List<ReviewCardViewModel>();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para operaciones inválidas
        {
            _logger.LogError(ex, "Error de operación inválida al cargar las reseñas. Detalles: {Message}", ex.Message);
            TempData["ErrorMessage"] = "No se pudieron procesar algunas operaciones al cargar las reseñas. Inténtalo más tarde.";
            PopularReviews = new List<ReviewCardViewModel>();
            RecentReviews = new List<ReviewCardViewModel>();
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al cargar las reseñas. Detalles: {Message}", ex.Message);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar las reseñas. Por favor, inténtalo de nuevo.";
            PopularReviews = new List<ReviewCardViewModel>();
            RecentReviews = new List<ReviewCardViewModel>();
        }
    }

    private async Task<ReviewCardViewModel> MapToViewModelAsync(ReviewDTO reviewDto)
    {
        if (reviewDto == null)
        {
            _logger.LogWarning("Se intentó mapear un ReviewDTO nulo a un ReviewCardViewModel.");
            return new ReviewCardViewModel
            {
                ReviewId = Guid.Empty.ToString(),
                GameId = Guid.Empty.ToString(),
                GameTitle = "N/A",
                GameImageUrl = "/images/noImage.png",
                UserId = "N/A",
                UserName = "Unknown User",
                UserAvatarUrl = "/images/default-avatar.png",
                Content = "No content available.",
                Rating = 0,
                LikesCount = 0,
                CreatedAt = DateTime.MinValue
            };
        }

        try
        {
            GamePreviewDTO? game = null;
            if (reviewDto.GameId != Guid.Empty)
            {
                game = await _gameService.GetGamePreviewByIdAsync(reviewDto.GameId);
            }
            else
            {
                _logger.LogWarning("ReviewDTO con ID '{ReviewId}' tiene un GameId vacío.", reviewDto.Id);
            }
            
            UserSearchResultDto? authUser = null;
            UserProfileDTO? profile = null;
            if (!string.IsNullOrWhiteSpace(reviewDto.UserId))
            {
                authUser = await _authService.SearchUserByIdAsync(reviewDto.UserId);
                profile = await _userManagerService.GetProfileAsync(reviewDto.UserId);
            }
            else
            {
                _logger.LogWarning("ReviewDTO con ID '{ReviewId}' tiene un UserId vacío.", reviewDto.Id);
            }

            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(),
                GameId = reviewDto.GameId.ToString(),
                GameTitle = game?.Title ?? "Unknown Game",
                GameImageUrl = game?.HeaderUrl ?? "/images/noImage.png",
                UserId = reviewDto.UserId ?? "N/A",
                UserName = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Unknown User",
                UserAvatarUrl = !string.IsNullOrWhiteSpace(profile?.AvatarUrl) ? profile.AvatarUrl : "/images/default-avatar.png",
                Content = reviewDto.Content ?? string.Empty,
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con servicios externos
        {
            _logger.LogError(ex, "Error de red al mapear la reseña '{ReviewId}'. Detalles: {Message}", reviewDto.Id, ex.Message);
            // Puedes devolver un ViewModel con un mensaje de error específico para este caso
            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(),
                GameId = reviewDto.GameId.ToString(),
                GameTitle = "Error de Conexión", 
                GameImageUrl = "/images/error.png",
                UserId = reviewDto.UserId ?? "N/A",
                UserName = "Error User",
                UserAvatarUrl = "/images/default-avatar.png",
                Content = "No se pudo cargar la información completa debido a un problema de conexión.",
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción durante el mapeo
        {
            _logger.LogError(ex, "Error inesperado al mapear la reseña '{ReviewId}'. Detalles: {Message}", reviewDto.Id, ex.Message);
            return new ReviewCardViewModel
            {
                ReviewId = reviewDto.Id ?? Guid.Empty.ToString(),
                GameId = reviewDto.GameId.ToString(),
                GameTitle = "Error General",
                GameImageUrl = "/images/error.png",
                UserId = reviewDto.UserId ?? "N/A",
                UserName = "Error User",
                UserAvatarUrl = "/images/default-avatar.png",
                Content = "Ocurrió un error inesperado al procesar la reseña.",
                Rating = reviewDto.Rating,
                LikesCount = reviewDto.Likes,
                CreatedAt = reviewDto.CreatedAt
            };
        }
    }
}