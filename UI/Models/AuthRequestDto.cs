using System.ComponentModel.DataAnnotations;
public class AuthRequestDto
{
    [Required(ErrorMessage = "El nombre de usuario o email es requerido.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 100 caracteres.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; } = false;
}