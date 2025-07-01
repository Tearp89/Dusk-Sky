using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // Necesario para HttpContext.SignOutAsync y CookieAuthenticationDefaults.AuthenticationScheme
using Microsoft.Extensions.Logging; // ¡Nuevo: Importante para el logging!



[Authorize] 
public class SettingsModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService; 
    private readonly ILogger<SettingsModel> _logger; 

    [BindProperty]
    public ChangePasswordViewModel ChangePasswordInput { get; set; } = new();

    [BindProperty]
    public DeleteAccountViewModel DeleteAccountInput { get; set; } = new();


    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    // Constructor
    public SettingsModel(
        IAuthService authService,
        IUserManagerService userManagerService,
        ILogger<SettingsModel> logger) 
    {
        _authService = authService;
        _userManagerService = userManagerService;
        _logger = logger; 
    }

    public void OnGet()
    {
        _logger.LogInformation("Accediendo a la página de Configuración (OnGet).");
        
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        _logger.LogInformation("Intento de cambio de contraseña.");

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("OnPostChangePasswordAsync: User ID no encontrado en los claims. Posible sesión inválida.");
            StatusMessage = "Error: Usuario no encontrado o sesión inválida. Por favor, vuelve a iniciar sesión.";
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("OnPostChangePasswordAsync: Errores de validación de modelo para el cambio de contraseña.");
            StatusMessage = "Error: Por favor, revisa los datos ingresados para cambiar tu contraseña.";
            return Page(); 
        }

        try
        {
            _logger.LogInformation("Intentando cambiar contraseña para el usuario: {UserId}", userId);
            var changePasswordSuccess = await _authService.ChangePasswordAsync(userId, ChangePasswordInput.OldPassword, ChangePasswordInput.NewPassword);

            if (changePasswordSuccess)
            {
                _logger.LogInformation("Contraseña cambiada exitosamente para el usuario: {UserId}. Deslogueando para reautenticación.", userId);
                StatusMessage = "Tu contraseña ha sido cambiada exitosamente. Por favor, vuelve a iniciar sesión con tu nueva contraseña.";
                
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 

                return RedirectToPage("/Startpage"); 
            }
            else
            {
                _logger.LogWarning("Falló el cambio de contraseña para el usuario {UserId}. Posibles credenciales incorrectas o problema del servicio de autenticación.", userId);
                ModelState.AddModelError(string.Empty, "Error al cambiar la contraseña. Por favor, verifica tu contraseña actual y vuelve a intentarlo.");
                StatusMessage = "Error: Falló el cambio de contraseña. Por favor, verifica tu contraseña actual y vuelve a intentarlo.";
                return Page();
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar cambiar la contraseña para el usuario {UserId}.", userId);
            StatusMessage = "Error de conexión: No se pudo conectar con el servicio de autenticación. Inténtalo de nuevo más tarde.";
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al intentar cambiar la contraseña para el usuario {UserId}.", userId);
            StatusMessage = "Ocurrió un error inesperado al cambiar la contraseña. Por favor, inténtalo de nuevo.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        _logger.LogInformation("Intento de eliminación de cuenta.");

        
        if (DeleteAccountInput.ConfirmDeletion != true)
        {
            _logger.LogWarning("OnPostDeleteAccountAsync: El usuario no confirmó la eliminación de la cuenta.");
            ModelState.AddModelError("DeleteAccountInput.ConfirmDeletion", "Debes confirmar para eliminar tu cuenta.");
            StatusMessage = "Error: La confirmación es requerida para eliminar la cuenta.";
            return Page();
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("OnPostDeleteAccountAsync: User ID no encontrado en los claims al intentar eliminar cuenta.");
            StatusMessage = "Error: Usuario no encontrado o sesión inválida. Por favor, vuelve a iniciar sesión.";
            return RedirectToPage();
        }

        try
        {
            _logger.LogInformation("Intentando eliminar perfil de usuario para {UserId}.", userId);
            var deleteProfileSuccess = await _userManagerService.DeleteProfileAsync(userId);
            if (!deleteProfileSuccess)
            {
                _logger.LogWarning("Falló la eliminación del perfil de usuario para {UserId}. El servicio indicó un problema.", userId);
                StatusMessage = "Error: Falló al eliminar los datos de tu perfil. Por favor, inténtalo de nuevo.";
                return Page();
            }
            _logger.LogInformation("Perfil de usuario eliminado exitosamente para {UserId}.", userId);

            _logger.LogInformation("Intentando eliminar cuenta de autenticación para {UserId}.", userId);
            var deleteAuthAccountSuccess = await _authService.DeleteAccountAsync(userId);
            if (!deleteAuthAccountSuccess)
            {
                _logger.LogWarning("Falló la eliminación de la cuenta de autenticación para {UserId}. El servicio indicó un problema.", userId);
                StatusMessage = "Error: Falló al eliminar tu cuenta de autenticación. Por favor, inténtalo de nuevo.";
                
                return Page();
            }
            _logger.LogInformation("Cuenta de autenticación eliminada exitosamente para {UserId}.", userId);

            
            _logger.LogInformation("Limpiando sesión y borrando cookie 'DuskSkyToken' para el usuario {UserId}.", userId);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogDebug("HttpContext.SignOutAsync ejecutado para el esquema de cookies.");

            UserSessionManager.Instance.ClearSession();
            _logger.LogDebug("UserSessionManager.Instance.ClearSession() ejecutado.");

            
            Response.Cookies.Delete("DuskSkyToken", new CookieOptions
            {
                Path = "/",
                Domain = HttpContext.Request.Host.Host, 
                SameSite = SameSiteMode.Strict,
                Secure = HttpContext.Request.IsHttps, 
                HttpOnly = true
            });
            _logger.LogInformation("Cookie 'DuskSkyToken' borrada.");

            StatusMessage = "Tu cuenta ha sido eliminada exitosamente.";
            return RedirectToPage("/StartPage");
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar eliminar la cuenta para el usuario {UserId}.", userId);
            StatusMessage = "Error de conexión: No se pudo conectar con los servicios. Inténtalo de nuevo más tarde.";
            return Page();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al intentar eliminar la cuenta para el usuario {UserId}.", userId);
            StatusMessage = "Ocurrió un error inesperado al eliminar la cuenta. Por favor, inténtalo de nuevo.";
            return Page();
        }
    }
}

