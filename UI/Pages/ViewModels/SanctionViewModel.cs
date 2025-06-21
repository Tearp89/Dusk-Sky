// SanctionViewModel ajustado para tu SanctionDTO real
using System.ComponentModel.DataAnnotations;

public class SanctionViewModel // Para crear nuevas sanciones y mostrar existentes
{
    public string Id { get; set; } = string.Empty; // Cambiado de SanctionId a Id

    [Required(ErrorMessage = "Sanctioned User ID is required.")]
    public string UserId { get; set; } = string.Empty; // Usamos UserId para coincidir con el DTO

    [Required(ErrorMessage = "Sanction Type is required.")]
    public string Type { get; set; } = string.Empty; // Usamos Type para coincidir con el DTO

    [Required(ErrorMessage = "Reason is required.")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start Date is required.")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    // Si tu API devuelve 'is_active' (calculado por la API), añádelo
    public bool IsActive { get; set; }
}


public class UserRoleViewModel // Para mostrar usuarios y sus roles
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class AddGameViewModel // Para el formulario de añadir juego
{
    [Required(ErrorMessage = "Steam App ID is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "App ID must be a positive number.")]
    public int SteamAppId { get; set; }

    public string? ImportStatusMessage { get; set; }
    public string? ImportedGameTitle { get; set; }
    public Guid? ImportedGameId { get; set; }
}

public class ReportDisplayViewModel // Para mostrar reportes en la UI
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportedUserId { get; set; } = string.Empty;
    public string ReportedUsername { get; set; } = string.Empty; 
    public string ReporterId { get; set; } = string.Empty; // Si tu API lo devuelve
    public string ReporterUsername { get; set; } = string.Empty; // Si tu API lo devuelve
    public string ContentType { get; set; } = string.Empty; // Ej. "comment", "review", "profile"
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Ej. "pending", "resolved"
    public DateTime ReportedAt { get; set; } 
    public DateTime? ResolvedAt { get; set; } // Si tu API lo devuelve
}