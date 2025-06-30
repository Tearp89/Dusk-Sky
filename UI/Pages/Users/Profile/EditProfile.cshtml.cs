using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Necesario para IFormFile
using System.IO; // Necesario para MemoryStream (si lo usas para IFormFile)
using System.Collections.Generic; // Para List<IFormFile> si tu UserProfileUploadDTO lo usa
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using
using System; // Para ArgumentNullException
using System.Net.Http; // ✅ Para HttpRequestException

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;

[Authorize]
public class EditProfileModel : PageModel
{
    private readonly IUserManagerService _userManagerService;
    private readonly IAuthService _authService;
    private readonly ILogger<EditProfileModel> _logger; // ✅ Declaración del logger

    [BindProperty]
    public EditProfileViewModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public EditProfileModel(
        IUserManagerService userManagerService,
        IAuthService authService,
        ILogger<EditProfileModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para los servicios y el logger
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
    }

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ✅ Validar loggedInUserId y si coincide con el userId de la ruta
        if (string.IsNullOrEmpty(loggedInUserId))
        {
            _logger.LogWarning("OnGetAsync: Usuario no autenticado intentó acceder a la edición de perfil."); // ✅ Registro de advertencia
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(userId) || userId != loggedInUserId)
        {
            _logger.LogWarning("OnGetAsync: Redirigiendo a '{CorrectUserId}' porque el userId de la ruta era '{RequestedUserId}' o nulo.", loggedInUserId, userId); // ✅ Registro de advertencia
            return RedirectToPage("/Users/EditProfile", new { userId = loggedInUserId });
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Cargando perfil para el usuario '{UserId}'.", loggedInUserId); // ✅ Registro de información
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            var authUser = await _authService.SearchUserByIdAsync(loggedInUserId);

            if (userProfile == null || authUser == null)
            {
                _logger.LogError("OnGetAsync: Perfil o datos de autenticación del usuario '{UserId}' no encontrados.", loggedInUserId); // ✅ Registro de error
                StatusMessage = $"Error: Unable to load user profile with ID '{loggedInUserId}'.";
                return NotFound();
            }

            Input.UserId = loggedInUserId;
            Input.Username = authUser.Username;
            Input.Bio = userProfile.Bio;
            Input.CurrentAvatarUrl = userProfile.AvatarUrl;
            Input.CurrentBannerUrl = userProfile.BannerUrl;

            _logger.LogInformation("OnGetAsync: Perfil del usuario '{UserId}' cargado exitosamente.", loggedInUserId); // ✅ Registro de información
            return Page();
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red con los servicios
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); // ✅ Registro de error
            StatusMessage = "Error de conexión al cargar su perfil. Por favor, inténtelo de nuevo.";
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); // ✅ Registro de error
            StatusMessage = "Error inesperado al cargar su perfil. Por favor, inténtelo de nuevo más tarde.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Seguridad: Asegurarse de que el usuario que envía el formulario sea el dueño del perfil
        if (string.IsNullOrEmpty(loggedInUserId) || Input.UserId != loggedInUserId)
        {
            _logger.LogWarning("OnPostAsync: Intento no autorizado de editar perfil. Usuario logueado: '{LoggedInUser}', ID de entrada: '{InputUserId}'.", loggedInUserId ?? "Nulo", Input.UserId); // ✅ Registro de advertencia
            StatusMessage = "Error: No estás autorizado para editar este perfil.";
            return RedirectToPage();
        }

        // Si hay errores de validación del modelo
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("OnPostAsync: Errores de validación de ModelState al actualizar perfil para el usuario '{UserId}'.", loggedInUserId); // ✅ Registro de advertencia
            StatusMessage = "Error: Por favor, corrija los errores en el formulario.";
            // Es crucial volver a cargar las URLs actuales si el modelo no es válido para que las previsualizaciones no se pierdan
            try
            {
                var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
                if (userProfile != null)
                {
                    Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                    Input.CurrentBannerUrl = userProfile.BannerUrl;
                }
                else
                {
                    _logger.LogError("OnPostAsync: Perfil del usuario '{UserId}' no encontrado al recargar datos después de error de validación.", loggedInUserId);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "OnPostAsync: HttpRequestException al recargar perfil después de ModelState error para el usuario '{UserId}'.", loggedInUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnPostAsync: Error inesperado al recargar perfil después de ModelState error para el usuario '{UserId}'.", loggedInUserId);
            }
            return Page();
        }

        bool overallSuccess = true;

        try
        {
            // --- 1. Actualizar Username ---
            var authUser = await _authService.SearchUserByIdAsync(loggedInUserId);
            if (authUser != null && authUser.Username != Input.Username)
            {
                _logger.LogInformation("OnPostAsync: Intentando cambiar nombre de usuario de '{OldUsername}' a '{NewUsername}' para el usuario '{UserId}'.", authUser.Username, Input.Username, loggedInUserId); // ✅ Registro de información
                var usernameUpdateSuccess = await _userManagerService.ChangeUsernameAsync(loggedInUserId, Input.Username);
                if (!usernameUpdateSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Error al actualizar el nombre de usuario. Podría estar en uso o ocurrió un error interno.");
                    StatusMessage = "Error: Fallo al actualizar nombre de usuario. Inténtelo de nuevo.";
                    _logger.LogWarning("OnPostAsync: Fallo al cambiar nombre de usuario para '{UserId}' a '{NewUsername}'.", loggedInUserId, Input.Username); // ✅ Registro de advertencia
                    overallSuccess = false;
                }
                else
                {
                    _logger.LogInformation("OnPostAsync: Nombre de usuario cambiado exitosamente a '{NewUsername}' para el usuario '{UserId}'.", Input.Username, loggedInUserId); // ✅ Registro de información
                }
            }

            // --- 2. Actualizar Bio y Archivos (Avatar/Banner) ---
            var uploadData = new UserProfileUploadDTO
            {
                Bio = Input.Bio,
                Avatar = Input.AvatarFile,
                Banner = Input.BannerFile,
                Media = new List<IFormFile>()
            };

            // ✅ Validar que uploadData no sea nulo (aunque ya se inicializa)
            if (uploadData == null)
            {
                _logger.LogError("OnPostAsync: uploadData es nulo inesperadamente para el usuario '{UserId}'.", loggedInUserId);
                StatusMessage = "Error interno al preparar los datos de carga del perfil.";
                overallSuccess = false;
            }
            else
            {
                _logger.LogInformation("OnPostAsync: Intentando actualizar contenido del perfil (Bio, Avatar, Banner) para el usuario '{UserId}'.", loggedInUserId); // ✅ Registro de información
                var updatedProfile = await _userManagerService.UploadProfileContentAsync(loggedInUserId, uploadData);
                if (updatedProfile == null)
                {
                    StatusMessage = "Error: Fallo al actualizar el contenido del perfil (bio, avatar, banner). Por favor, inténtelo de nuevo.";
                    _logger.LogError("OnPostAsync: Fallo en UploadProfileContentAsync para el usuario '{UserId}'. El servicio devolvió null.", loggedInUserId); // ✅ Registro de error
                    overallSuccess = false;
                }
                else
                {
                    _logger.LogInformation("OnPostAsync: Contenido del perfil actualizado exitosamente para el usuario '{UserId}'.", loggedInUserId); // ✅ Registro de información
                }
            }

            if (overallSuccess)
            {
                StatusMessage = "¡Su perfil ha sido actualizado exitosamente!";
                return RedirectToPage("/Users/Profile/Index", new { userId = loggedInUserId });
            }
            else
            {
                // Si hubo algún error, recargar las URLs actuales del perfil para la previsualización
                var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
                if (userProfile != null)
                {
                    Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                    Input.CurrentBannerUrl = userProfile.BannerUrl;
                }
                else
                {
                    _logger.LogError("OnPostAsync: Perfil del usuario '{UserId}' no encontrado al recargar datos después de un error general.", loggedInUserId);
                }
                return Page();
            }
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red
        {
            _logger.LogError(ex, "OnPostAsync: HttpRequestException al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); // ✅ Registro de error
            StatusMessage = "Error de conexión al actualizar su perfil. Por favor, inténtelo de nuevo.";
            // Recargar datos para la vista
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null)
            {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page();
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para operaciones inválidas
        {
            _logger.LogError(ex, "OnPostAsync: InvalidOperationException al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); // ✅ Registro de error
            StatusMessage = "Operación inválida al actualizar su perfil. Por favor, inténtelo de nuevo.";
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null)
            {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page();
        }
        catch (Exception ex) // ✅ Catch general
        {
            _logger.LogError(ex, "OnPostAsync: Error inesperado al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); // ✅ Registro de error
            StatusMessage = "Ocurrió un error inesperado al actualizar su perfil. Por favor, inténtelo de nuevo más tarde.";
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null)
            {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page();
        }
    }
}

// Asegúrate de que tus DTOs y ViewModels estén definidos
// public class EditProfileViewModel
// {
//     public string UserId { get; set; } = string.Empty;
//     public string Username { get; set; } = string.Empty;
//     public string Bio { get; set; } = string.Empty;
//     public IFormFile? AvatarFile { get; set; }
//     public IFormFile? BannerFile { get; set; }
//     public string? CurrentAvatarUrl { get; set; }
//     public string? CurrentBannerUrl { get; set; }
// }

// public class UserProfileUploadDTO
// {
//     public string Bio { get; set; } = string.Empty;
//     public IFormFile? Avatar { get; set; }
//     public IFormFile? Banner { get; set; }
//     public List<IFormFile> Media { get; set; } = new();
// }

// public class UserProfileDTO
// {
//     public string AvatarUrl { get; set; } = string.Empty;
//     public string BannerUrl { get; set; } = string.Empty;
//     public string Bio { get; set; } = string.Empty;
//     // ... otras propiedades
// }

// public class AuthUserDTO
// {
//     public string Username { get; set; } = string.Empty;
//     // ... otras propiedades
// }