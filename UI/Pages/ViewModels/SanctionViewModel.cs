// SanctionViewModel ajustado para tu SanctionDTO real
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

public class SanctionViewModel // Para crear nuevas sanciones y mostrar existentes
{
    public string Id { get; set; } = string.Empty; // Cambiado de SanctionId a Id

    [Required(ErrorMessage = "Sanctioned User ID is required.")]
    public string UserId { get; set; } = string.Empty; // Usamos UserId para coincidir con el DTO

    [Required(ErrorMessage = "Sanction Type is required.")]
    public SanctionType Type { get; set; }


    [Required(ErrorMessage = "Reason is required.")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start Date is required.")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }
    public string Username { get; set; }

    // Si tu API devuelve 'is_active' (calculado por la API), añádelo
    public bool IsActive { get; set; }
    [ValidateNever] // No se valida directamente en el formulario, es un dato auxiliar
    public string? ReportId { get; set; }
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
    [Required(ErrorMessage = "Steam game link is required.")]

    public string SteamLink { get; set; } = string.Empty;

    public string? ImportStatusMessage { get; set; }
    public string? ImportedGameTitle { get; set; }
    public Guid? ImportedGameId { get; set; }
}

public class ReportDisplayViewModel
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportedUserId { get; set; } = string.Empty;
    public string ReportedUsername { get; set; } = string.Empty; // <-- AÑADE ESTA PROPIEDAD
    public string ReporterId { get; set; } = string.Empty; // Asumo que el reportero también es relevante
    public string ReporterUsername { get; set; } = string.Empty; // <-- Puedes añadir esta también para el reportero
    public string ContentType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReportedAt { get; set; }
}

public class PromoteUserInput // Nuevo ViewModel para PromoteUser
{
    [Required(ErrorMessage = "El ID del usuario a promover es requerido.")]
    public string UserId { get; set; } = string.Empty;
}

public class DemoteUserInput // Nuevo ViewModel para DemoteUser
{
    [Required(ErrorMessage = "El ID del usuario a degradar es requerido.")]
    public string UserId { get; set; } = string.Empty;
}

