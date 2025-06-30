using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // Necesario para HttpContext.SignOutAsync y CookieAuthenticationDefaults.AuthenticationScheme
using Microsoft.Extensions.Logging; // ¡Nuevo: Importante para el logging!

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;
// using YourApp.Utilities; // Si UserSessionManager está en una utilidad

[Authorize] // Solo usuarios autenticados pueden acceder a la configuración
public class SettingsModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService; // Para eliminar perfil de usuario
    private readonly ILogger<SettingsModel> _logger; // ¡Nuevo: Inyección del logger!

    // ViewModels para los formularios
    [BindProperty]
    public ChangePasswordViewModel ChangePasswordInput { get; set; } = new();

    [BindProperty]
    public DeleteAccountViewModel DeleteAccountInput { get; set; } = new();

    // Mensajes de estado para la UI
    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    // Constructor
    public SettingsModel(
        IAuthService authService,
        IUserManagerService userManagerService,
        ILogger<SettingsModel> logger) // ¡Nuevo: Parámetro del logger en el constructor!
    {
        _authService = authService;
        _userManagerService = userManagerService;
        _logger = logger; // Asignación del logger
    }

    public void OnGet()
    {
        _logger.LogInformation("Accediendo a la página de Configuración (OnGet).");
        // No se necesita cargar datos en el GET para esta página,
        // los formularios se inicializan con valores por defecto.
    }

    // --- Manejar el cambio de contraseña (POST) ---
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

        // Validación de ModelState (por ejemplo, si NewPassword no coincide con ConfirmNewPassword)
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("OnPostChangePasswordAsync: Errores de validación de modelo para el cambio de contraseña.");
            // Los errores de validación de modelo se mostrarán en la UI automáticamente si usas asp-validation-for/summary
            StatusMessage = "Error: Por favor, revisa los datos ingresados para cambiar tu contraseña.";
            return Page(); // Vuelve a la página para mostrar los errores de validación
        }

        try
        {
            _logger.LogInformation("Intentando cambiar contraseña para el usuario: {UserId}", userId);
            var changePasswordSuccess = await _authService.ChangePasswordAsync(userId, ChangePasswordInput.OldPassword, ChangePasswordInput.NewPassword);

            if (changePasswordSuccess)
            {
                _logger.LogInformation("Contraseña cambiada exitosamente para el usuario: {UserId}. Deslogueando para reautenticación.", userId);
                StatusMessage = "Tu contraseña ha sido cambiada exitosamente. Por favor, vuelve a iniciar sesión con tu nueva contraseña.";
                
                // Desloguear al usuario para que la cookie de autenticación se limpie y se vea forzado a iniciar sesión de nuevo.
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); 

                return RedirectToPage("/Startpage"); // Redirigir a la página de inicio de sesión o bienvenida
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
        catch (Exception ex) // Captura cualquier otra excepción inesperada
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al intentar cambiar la contraseña para el usuario {UserId}.", userId);
            StatusMessage = "Ocurrió un error inesperado al cambiar la contraseña. Por favor, inténtalo de nuevo.";
            return Page();
        }
    }

    // --- Manejar la eliminación de cuenta (POST) ---
    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        _logger.LogInformation("Intento de eliminación de cuenta.");

        // --- VALIDACIÓN MANUAL ---
        // Se mantiene tu validación manual existente para ConfirmDeletion
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
                // En un escenario real, si el perfil se borra pero la cuenta no, esto podría ser un problema serio de inconsistencia.
                // Podrías considerar un mecanismo de compensación o revertir el borrado del perfil.
                return Page();
            }
            _logger.LogInformation("Cuenta de autenticación eliminada exitosamente para {UserId}.", userId);

            // --- Lógica de borrado de sesión y cookie ---
            _logger.LogInformation("Limpiando sesión y borrando cookie 'DuskSkyToken' para el usuario {UserId}.", userId);

            // Desloguear al usuario de la autenticación de cookies
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogDebug("HttpContext.SignOutAsync ejecutado para el esquema de cookies.");

            // Limpiar la sesión personalizada (UserSessionManager)
            UserSessionManager.Instance.ClearSession();
            _logger.LogDebug("UserSessionManager.Instance.ClearSession() ejecutado.");

            // Borrar la cookie específica "DuskSkyToken"
            // Es crucial que los CookieOptions coincidan con cómo se creó la cookie originalmente.
            // Si el dominio y la seguridad son diferentes en producción, ajústalos.
            Response.Cookies.Delete("DuskSkyToken", new CookieOptions
            {
                Path = "/",
                Domain = HttpContext.Request.Host.Host, // Usa el host actual para mayor portabilidad
                SameSite = SameSiteMode.Strict,
                Secure = HttpContext.Request.IsHttps, // Usa true solo si estás en HTTPS
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
        catch (Exception ex) // Captura cualquier otra excepción inesperada
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al intentar eliminar la cuenta para el usuario {UserId}.", userId);
            StatusMessage = "Ocurrió un error inesperado al eliminar la cuenta. Por favor, inténtalo de nuevo.";
            return Page();
        }
    }
}

