using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http; // Necesario para IFormFile
using System.IO; // Necesario para MemoryStream (si lo usas para IFormFile)
using System.Collections.Generic; // Para List<IFormFile> si tu UserProfileUploadDTO lo usa

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels;

[Authorize]
public class EditProfileModel : PageModel
{
    private readonly IUserManagerService _userManagerService;
    private readonly IAuthService _authService;

    [BindProperty]
    public EditProfileViewModel Input { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public EditProfileModel(
        IUserManagerService userManagerService,
        IAuthService authService)
    {
        _userManagerService = userManagerService;
        _authService = authService;
    }

    public async Task<IActionResult> OnGetAsync(string? userId)
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId) || userId != loggedInUserId)
        {
            return RedirectToPage("/Users/EditProfile", new { userId = loggedInUserId });
        }

        var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
        var authUser = await _authService.SearchUserByIdAsync(loggedInUserId);

        if (userProfile == null || authUser == null)
        {
            return NotFound($"Unable to load user profile with ID '{loggedInUserId}'.");
        }

        Input.UserId = loggedInUserId;
        Input.Username = authUser.Username;
        Input.Bio = userProfile.Bio;
        // Asignar las URLs actuales para previsualización
        Input.CurrentAvatarUrl = userProfile.AvatarUrl;
        Input.CurrentBannerUrl = userProfile.BannerUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Seguridad: Asegurarse de que el usuario que envía el formulario sea el dueño del perfil
        if (Input.UserId != loggedInUserId)
        {
            StatusMessage = "Error: You are not authorized to edit this profile.";
            return RedirectToPage();
        }

        // Si hay errores de validación (por ejemplo, username muy corto)
        if (!ModelState.IsValid)
        {
            StatusMessage = "Error: Please correct the errors in the form.";
            // Es crucial volver a cargar las URLs actuales si el modelo no es válido
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null) {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page(); 
        }

        bool overallSuccess = true;

        // --- 1. Actualizar Username ---
        var authUser = await _authService.SearchUserByIdAsync(loggedInUserId); // Obtener el username actual para comparar
        if (authUser != null && authUser.Username != Input.Username)
        {
            var usernameUpdateSuccess = await _userManagerService.ChangeUsernameAsync(loggedInUserId, Input.Username);
            if (!usernameUpdateSuccess)
            {
                ModelState.AddModelError(string.Empty, "Failed to update username. It might be taken or an internal error occurred.");
                StatusMessage = "Error: Failed to update username. Please try again.";
                overallSuccess = false;
            }
        }

        // --- 2. Actualizar Bio y Archivos (Avatar/Banner) ---
        // Preparamos el DTO para el endpoint /upload
        var uploadData = new UserProfileUploadDTO
        {
            Bio = Input.Bio,
            // Solo asignamos los archivos si han sido seleccionados
            Avatar = Input.AvatarFile,
            Banner = Input.BannerFile,
            Media = new List<IFormFile>() // Tu DTO espera una lista, aunque no la usemos aquí
            // about_section no está en tu EditProfileViewModel, así que no se asigna.
        };
        
        // Llama al endpoint PATCH profiles/{user_id}/upload
        var updatedProfile = await _userManagerService.UploadProfileContentAsync(loggedInUserId, uploadData);
        if (updatedProfile == null)
        {
            StatusMessage = "Error: Failed to update profile content (bio, avatar, banner). Please try again.";
            overallSuccess = false;
        }

        if (overallSuccess)
        {
            StatusMessage = "Your profile has been updated successfully!";
            return RedirectToPage("/Users/Profile/Index", new { userId = loggedInUserId });
        }
        else
        {
            // Si hay un error, volvemos a cargar las URLs de las imágenes para que las previsualizaciones no se pierdan
            var userProfile = await _userManagerService.GetProfileAsync(loggedInUserId);
            if (userProfile != null) {
                Input.CurrentAvatarUrl = userProfile.AvatarUrl;
                Input.CurrentBannerUrl = userProfile.BannerUrl;
            }
            return Page(); 
        }
    }
}