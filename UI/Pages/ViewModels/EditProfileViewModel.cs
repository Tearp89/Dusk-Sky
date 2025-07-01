using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Necesario para IFormFile

public class EditProfileViewModel
{
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters.")]
    public string Username { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
    public string Bio { get; set; } = string.Empty;

    // Campos para la SUBIDA DE ARCHIVOS
    [Display(Name = "Upload Avatar")]
    public IFormFile? AvatarFile { get; set; } // Archivo para el avatar

    [Display(Name = "Upload Banner")]
    public IFormFile? BannerFile { get; set; } // Archivo para el banner

    // Opcional: Para mostrar las URLs actuales (no se bindearán en el POST, solo para GET)
    public string CurrentAvatarUrl { get; set; } = string.Empty;
    public string CurrentBannerUrl { get; set; } = string.Empty;

    public string? StatusMessage { get; set; }
}