using System.ComponentModel.DataAnnotations; // Asegúrate de incluir este using

public class RegisterRequestDto
{
    [Required(ErrorMessage = "El email es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    [StringLength(100, ErrorMessage = "El email no puede exceder los 100 caracteres.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "El nombre de usuario es requerido.")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "La contraseña es requerida.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "La confirmación de contraseña es requerida.")] // Campo requerido
    [DataType(DataType.Password)] // Para que el input sea de tipo password
    [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")] // Valida que sea igual a 'Password'
    public string ConfirmPassword { get; set; } = ""; // Nueva propiedad

    public bool RememberMe { get; set; } = false;
}