using Microsoft.AspNetCore.Authorization;
// REMOVER: using Microsoft.AspNetCore.Identity; // Ya no necesario si no usas ASP.NET Core Identity
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication; // <-- ¡Necesario para HttpContext.SignOutAsync!

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;

[Authorize] // Solo usuarios autenticados pueden acceder a la configuración
public class SettingsModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService; // Para eliminar perfil de usuario
    // REMOVER: private readonly SignInManager<ApplicationUser> _signInManager; // Ya no necesario

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
        IUserManagerService userManagerService
        /* REMOVER: , SignInManager<ApplicationUser> signInManager */ ) 
    {
        _authService = authService;
        _userManagerService = userManagerService;
        // REMOVER: _signInManager = signInManager;
    }

    public void OnGet()
    {
        // No se necesita cargar datos en el GET para esta página,
        // los formularios se inicializan con valores por defecto.
    }

    // --- Manejar el cambio de contraseña (POST) ---
    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
       

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            StatusMessage = "Error: User not found.";
            return RedirectToPage();
        }

        var changePasswordSuccess = await _authService.ChangePasswordAsync(userId, ChangePasswordInput.OldPassword, ChangePasswordInput.NewPassword);

        if (changePasswordSuccess)
        {
            StatusMessage = "Your password has been changed successfully!";
            // ¡AJUSTE AQUÍ para actualizar la cookie de autenticación!
            // Para actualizar los claims después de un cambio de contraseña,
            // la forma más sencilla es volver a iniciar sesión al usuario.
            // Esto implica obtener el token/claims de nuevo tras el cambio de contraseña exitoso.
            
            // Opción 1 (Más fácil): Simplemente desloguear y pedir que se loguee de nuevo.
            await HttpContext.SignOutAsync("Cookies"); // Especifica el esquema de autenticación

            // Opción 2 (Más compleja): Re-autenticar con nuevos claims si tu AuthService devuelve el token/claims.
            // Si ChangePasswordAsync no devuelve el nuevo token/claims, tendrías que llamar a LoginAsync de nuevo.
            // Por simplicidad, la Opción 1 es más fácil.
            // var newAuthResponse = await _authService.LoginAsync(new AuthRequestDto { Username = User.Identity.Name, Password = ChangePasswordInput.NewPassword });
            // if (newAuthResponse != null) { /* ... crear claims y HttpContext.SignInAsync ... */ }

            return RedirectToPage("/Startpage"); // Redirigir a la página de login
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Error changing password. Please check your current password and try again.");
            StatusMessage = "Error: Failed to change password. Please check your current password and try again.";
            return Page();
        }
    }

    // --- Manejar la eliminación de cuenta (POST) ---
    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        if (!ModelState.IsValid) // Solo si DeleteAccountInput tiene validaciones
        {
            StatusMessage = "Error: Please confirm account deletion.";
            return Page();
        }

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            StatusMessage = "Error: User not found.";
            return RedirectToPage();
        }

        var deleteProfileSuccess = await _userManagerService.DeleteProfileAsync(userId);
        if (!deleteProfileSuccess)
        {
            StatusMessage = "Error: Failed to delete user profile data.";
            return Page();
        }

        var deleteAuthAccountSuccess = await _authService.DeleteAccountAsync(userId);
        if (!deleteAuthAccountSuccess)
        {
            StatusMessage = "Error: Failed to delete authentication account.";
            return Page();
        }

        // Desloguear al usuario después de eliminar la cuenta
        await HttpContext.SignOutAsync("Cookies"); // Especifica el esquema de autenticación "Cookies"

        StatusMessage = "Your account has been successfully deleted.";
        return RedirectToPage("/Index"); // Redirigir a la página de inicio o a una página de confirmación
    }
}