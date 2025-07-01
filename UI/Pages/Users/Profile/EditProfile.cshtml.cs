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
    private readonly ILogger<EditProfileModel> _logger; 

    [BindProperty]
    public EditProfileViewModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public EditProfileModel(
        IUserManagerService userManagerService,
        IAuthService authService,
        ILogger<EditProfileModel> logger) 
    {
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "IUserManagerService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "IAuthService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
    }

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(loggedInUserId))
        {
            _logger.LogWarning("OnGetAsync: Usuario no autenticado intentó acceder a la edición de perfil."); 
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(userId) || userId != loggedInUserId)
        {
            _logger.LogWarning("OnGetAsync: Redirigiendo a '{CorrectUserId}' porque el userId de la ruta era '{RequestedUserId}' o nulo.", loggedInUserId, userId); 
            return RedirectToPage("/Users/EditProfile", new { userId = loggedInUserId });
        }

        try
        {
            _logger.LogInformation("OnGetAsync: Cargando perfil para el usuario '{UserId}'.", loggedInUserId); 
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            var authUser = await _authService.SearchUserByIdAsync(loggedInUserId);

            if (userProfile == null || authUser == null)
            {
                _logger.LogError("OnGetAsync: Perfil o datos de autenticación del usuario '{UserId}' no encontrados.", loggedInUserId); 
                StatusMessage = $"Error: Unable to load user profile with ID '{loggedInUserId}'.";
                return NotFound();
            }

            Input.UserId = loggedInUserId;
            Input.Username = authUser.Username;
            Input.Bio = userProfile.Bio;
            Input.CurrentAvatarUrl = userProfile.AvatarUrl;
            Input.CurrentBannerUrl = userProfile.BannerUrl;

            _logger.LogInformation("OnGetAsync: Perfil del usuario '{UserId}' cargado exitosamente.", loggedInUserId); 
            return Page();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnGetAsync: HttpRequestException al cargar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); 
            StatusMessage = "Error de conexión al cargar su perfil. Por favor, inténtelo de nuevo.";
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnGetAsync: Error inesperado al cargar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); 
            StatusMessage = "Error inesperado al cargar su perfil. Por favor, inténtelo de nuevo más tarde.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(loggedInUserId) || Input.UserId != loggedInUserId)
        {
            _logger.LogWarning("OnPostAsync: Intento no autorizado de editar perfil. Usuario logueado: '{LoggedInUser}', ID de entrada: '{InputUserId}'.", loggedInUserId ?? "Nulo", Input.UserId); 
            StatusMessage = "Error: No estás autorizado para editar este perfil.";
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("OnPostAsync: Errores de validación de ModelState al actualizar perfil para el usuario '{UserId}'.", loggedInUserId); 
            StatusMessage = "Error: Por favor, corrija los errores en el formulario.";
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
            var authUser = await _authService.SearchUserByIdAsync(loggedInUserId);
            if (authUser != null && authUser.Username != Input.Username)
            {
                _logger.LogInformation("OnPostAsync: Intentando cambiar nombre de usuario de '{OldUsername}' a '{NewUsername}' para el usuario '{UserId}'.", authUser.Username, Input.Username, loggedInUserId); 
                var usernameUpdateSuccess = await _userManagerService.ChangeUsernameAsync(loggedInUserId, Input.Username);
                if (!usernameUpdateSuccess)
                {
                    ModelState.AddModelError(string.Empty, "Error al actualizar el nombre de usuario. Podría estar en uso o ocurrió un error interno.");
                    StatusMessage = "Error: Fallo al actualizar nombre de usuario. Inténtelo de nuevo.";
                    _logger.LogWarning("OnPostAsync: Fallo al cambiar nombre de usuario para '{UserId}' a '{NewUsername}'.", loggedInUserId, Input.Username); 
                    overallSuccess = false;
                }
                else
                {
                    _logger.LogInformation("OnPostAsync: Nombre de usuario cambiado exitosamente a '{NewUsername}' para el usuario '{UserId}'.", Input.Username, loggedInUserId); 
                }
            }

            var uploadData = new UserProfileUploadDTO
            {
                Bio = Input.Bio,
                Avatar = Input.AvatarFile,
                Banner = Input.BannerFile,
                Media = new List<IFormFile>()
            };

            if (uploadData == null)
            {
                _logger.LogError("OnPostAsync: uploadData es nulo inesperadamente para el usuario '{UserId}'.", loggedInUserId);
                StatusMessage = "Error interno al preparar los datos de carga del perfil.";
                overallSuccess = false;
            }
            else
            {
                _logger.LogInformation("OnPostAsync: Intentando actualizar contenido del perfil (Bio, Avatar, Banner) para el usuario '{UserId}'.", loggedInUserId); 
                var updatedProfile = await _userManagerService.UploadProfileContentAsync(loggedInUserId, uploadData);
                if (updatedProfile == null)
                {
                    StatusMessage = "Error: Fallo al actualizar el contenido del perfil (bio, avatar, banner). Por favor, inténtelo de nuevo.";
                    _logger.LogError("OnPostAsync: Fallo en UploadProfileContentAsync para el usuario '{UserId}'. El servicio devolvió null.", loggedInUserId); 
                    overallSuccess = false;
                }
                else
                {
                    _logger.LogInformation("OnPostAsync: Contenido del perfil actualizado exitosamente para el usuario '{UserId}'.", loggedInUserId); 
                }
            }

            if (overallSuccess)
            {
                StatusMessage = "¡Su perfil ha sido actualizado exitosamente!";
                return RedirectToPage("/Users/Profile/Index", new { userId = loggedInUserId });
            }
            else
            {
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
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "OnPostAsync: HttpRequestException al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); 
            StatusMessage = "Error de conexión al actualizar su perfil. Por favor, inténtelo de nuevo.";
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null)
            {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page();
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "OnPostAsync: InvalidOperationException al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); 
            StatusMessage = "Operación inválida al actualizar su perfil. Por favor, inténtelo de nuevo.";
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null)
            {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "OnPostAsync: Error inesperado al actualizar perfil para el usuario '{UserId}'. Mensaje: {ErrorMessage}", loggedInUserId, ex.Message); 
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

