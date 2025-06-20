using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Text.Json.Serialization; // Para JsonPropertyName en los DTOs de este archivo

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels; // donde están tus AdminViewModels, UserRoleViewModel, etc.

// =========================================================
// DTOs y ViewModels específicos para este PageModel
// Puedes poner estos en un archivo AdminViewModels.cs
// o dentro de este mismo archivo como clases anidadas.
// =========================================================



// DTO para enviar a la API para crear una sanción
public class SanctionCreateDTO 
{
    [JsonPropertyName("user_id")] // Mapea al campo 'user_id' en FastAPI
    public string UserId { get; set; } = string.Empty;
    
    [JsonPropertyName("type")] // Mapea al campo 'type' en FastAPI
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;
    
    [JsonPropertyName("start_date")]
    public DateTime StartDate { get; set; }
    
    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }
}

// DTOs de servicio (deben coincidir con tus servicios y API)
// Esto debe estar en tus archivos de DTOs globales (ej. DTOs/ReportDTO.cs)
/*
public class ReportDTO // Desde IModerationReportService
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("reported_user_id")] public string ReportedUserId { get; set; } = string.Empty;
    [JsonPropertyName("reporter_id")] public string ReporterId { get; set; } = string.Empty; // Si tu API lo da
    [JsonPropertyName("content_type")] public string ContentType { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("reported_at")] public DateTime ReportedAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("resolved_at")] public DateTime? ResolvedAt { get; set; } // Si tu API lo da
}

public class SanctionDTO // Desde IModerationSanctionService
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("report_id")] public string? ReportId { get; set; }
    [JsonPropertyName("user_id")] public string UserId { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("start_date")] public DateTime StartDate { get; set; }
    [JsonPropertyName("end_date")] public DateTime? EndDate { get; set; }
    // [JsonPropertyName("is_active")] public bool IsActive { get; set; } // Si tu API lo da
}
*/
// =========================================================

[Authorize(Roles = "admin,moderator")] // Solo Admins y Moderadores pueden acceder
public class AdminDashboardModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IGameService _gameService;
    private readonly IModerationReportService _reportService;
    private readonly IModerationSanctionService _sanctionService;
    private readonly IUserManagerService _userManagerService; 

    // Propiedades del Modelo para la vista
    public List<UserRoleViewModel> AllUsers { get; set; } = new();
    public List<ReportDisplayViewModel> RecentReports { get; set; } = new();
    public List<SanctionViewModel> ActiveSanctions { get; set; } = new();

    // Modelos para formularios (BindProperty)
    [BindProperty]
    public AddGameViewModel AddGameInput { get; set; } = new();

    [BindProperty]
    public SanctionViewModel CreateSanctionInput { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;

    public AdminDashboardModel(
        IAuthService authService,
        IGameService gameService,
        IModerationReportService reportService,
        IModerationSanctionService sanctionService,
        IUserManagerService userManagerService)
    {
        _authService = authService;
        _gameService = gameService;
        _reportService = reportService;
        _sanctionService = sanctionService;
        _userManagerService = userManagerService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        // Cargar todos los reportes (Admin y Moderador)
        await LoadReportsAsync();
        // Cargar todas las sanciones (Admin y Moderador)
        await LoadSanctionsAsync();

        // Cargar todos los usuarios (SOLO ADMIN)
        if (User.IsInRole("admin"))
        {
            await LoadAllUsersAsync();
        }

        return Page();
    }

    // --- Métodos de carga de datos ---

    private async Task LoadAllUsersAsync()
    {
        // Este método necesita un endpoint en tu backend de autenticación que liste todos los usuarios.
        // Asumo que tu IAuthService tiene un método GetAllUsersAsync() o similar que devuelve List<UserSearchResultDto>
        // Si no lo tienes, UserSearchResultDto necesitaría una propiedad para los roles.
        // Por ahora, si no tienes un GetAllUsersAsync, esta sección no se llenará.
        // O podrías buscar los usuarios que tienen rol 'Admin' o 'Moderator' específicamente.

        // Placeholder si no tienes un GetAllUsersAsync
        // AllUsers = new List<UserRoleViewModel>(); 

        // Si tienes un método GetAllUsersAsync en tu IAuthService:
        // var allAuthUsers = await _authService.GetAllUsersAsync(); // Este método no existe en tu IAuthService actual
        // if (allAuthUsers != null) {
        //     var userTasks = allAuthUsers.Select(async u => {
        //         var profile = await _userManagerService.GetProfileAsync(u.Id); // Para el avatar
        //         return new UserRoleViewModel {
        //             UserId = u.Id,
        //             Username = u.Username,
        //             Roles = u.Roles, // Asumo que UserSearchResultDto tiene una lista de Roles
        //             AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
        //         };
        //     });
        //     AllUsers = (await Task.WhenAll(userTasks)).ToList();
        // }
    }

    private async Task LoadReportsAsync()
    {
        var reports = await _reportService.GetAllAsync();
        if (reports != null)
        {
            var displayTasks = reports.Select(async r =>
            {
                // Obtener usernames de los IDs
                var reportedUserTask = _authService.SearchUserByIdAsync(r.ReportedUserId);

                await Task.WhenAll(reportedUserTask);

                var reportedUser = reportedUserTask.Result;

                return new ReportDisplayViewModel
                {
                    ReportId = r.Id,
                    ReportedUserId = r.ReportedUserId,
                    ReportedUsername = reportedUser?.Username ?? "Unknown User",
                    ContentType = r.ContentType, 
                    Reason = r.Reason,
                    Status = r.Status,
                    ReportedAt = r.ReportedAt 
                };
            });
            RecentReports = (await Task.WhenAll(displayTasks)).OrderByDescending(r => r.ReportedAt).ToList(); 
        }
    }

    private async Task LoadSanctionsAsync()
    {
        var sanctions = await _sanctionService.GetAllAsync();
        if (sanctions != null)
        {
            var displayTasks = sanctions.Select(async s =>
            {
                var sanctionedUser = await _authService.SearchUserByIdAsync(s.UserId); 
                // Determinar si la sanción está activa
                bool isActive = s.EndDate == null || s.EndDate > DateTime.UtcNow; 

                return new SanctionViewModel 
                {
                    Id = s.Id, 
                    UserId = s.UserId, 
                    Type = s.Type, 
                    Reason = s.Reason,
                    StartDate = s.StartDate,
                    EndDate = s.EndDate,
                    IsActive = isActive 
                };
            });
            ActiveSanctions = (await Task.WhenAll(displayTasks)).OrderByDescending(s => s.StartDate).ToList();
        }
    }


    // --- Métodos OnPost (Acciones de Admin/Moderador) ---

    // Acciones de Admin
    [Authorize(Roles = "admin")] // Solo Admin puede agregar juegos
    public async Task<IActionResult> OnPostAddGameAsync()
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Error: Please provide a valid Steam App ID.";
            return Page();
        }

        var (success, message, gameId) = await _gameService.ImportGameAsync(AddGameInput.SteamAppId);
        
        if (success)
        {
            StatusMessage = $"Success: {message}";
            AddGameInput.ImportStatusMessage = message;
            AddGameInput.ImportedGameTitle = "Game Imported"; 
            AddGameInput.ImportedGameId = gameId; 
        }
        else
        {
            StatusMessage = $"Error: {message}";
            AddGameInput.ImportStatusMessage = message;
        }

        // Recargar datos necesarios para que la página se vea completa después del POST
        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("Admin")) { await LoadAllUsersAsync(); }

        return Page();
    }

    [Authorize(Roles = "admin")] // Solo Admin puede promover/degradar
    public async Task<IActionResult> OnPostPromoteUserAsync(string userIdToPromote)
    {
        var success = await _authService.PromoteUserAsync(userIdToPromote);
        if (success)
        {
            StatusMessage = $"User {userIdToPromote} promoted to Moderator.";
        }
        else
        {
            StatusMessage = $"Error: Failed to promote user {userIdToPromote}.";
        }
        // Recargar datos para reflejar los cambios
        await LoadReportsAsync();
        await LoadSanctionsAsync();
        await LoadAllUsersAsync();
        return Page();
    }

    [Authorize(Roles = "admin")] // Solo Admin puede promover/degradar
    public async Task<IActionResult> OnPostDemoteUserAsync(string userIdToDemote)
    {
        var success = await _authService.DemoteUserAsync(userIdToDemote);
        if (success)
        {
            StatusMessage = $"User {userIdToDemote} demoted from Moderator.";
        }
        else
        {
            StatusMessage = $"Error: Failed to demote user {userIdToDemote}.";
        }
        // Recargar datos para reflejar los cambios
        await LoadReportsAsync();
        await LoadSanctionsAsync();
        await LoadAllUsersAsync();
        return Page();
    }

    // Acciones de Admin/Moderador (para reportes)
    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostResolveReportAsync(string reportId, string newStatus)
    {
        var report = await _reportService.GetByIdAsync(reportId);
        if (report == null)
        {
            StatusMessage = "Error: Report not found.";
            return Page();
        }
        report.Status = newStatus; 
        var success = await _reportService.UpdateAsync(reportId, report);
        if (success)
        {
            StatusMessage = $"Report {reportId} marked as {newStatus}.";
        }
        else
        {
            StatusMessage = $"Error: Failed to update report status.";
        }
        await LoadReportsAsync(); 
        return Page();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostDeleteReportAsync(string reportId)
    {
        var success = await _reportService.DeleteAsync(reportId);
        if (success)
        {
            StatusMessage = $"Report {reportId} deleted.";
        }
        else
        {
            StatusMessage = $"Error: Failed to delete report.";
        }
        await LoadReportsAsync(); 
        return Page();
    }

    // Acciones de Admin/Moderador (para sanciones)
    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostCreateSanctionAsync()
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Error: Please correct the errors in the sanction form.";
            return Page();
        }
        
        var sanctionDto = new SanctionDTO 
        {
            UserId = CreateSanctionInput.UserId, 
            Type = CreateSanctionInput.Type, 
            Reason = CreateSanctionInput.Reason,
            StartDate = CreateSanctionInput.StartDate,
            EndDate = CreateSanctionInput.EndDate,
        };

        var success = await _sanctionService.CreateAsync(sanctionDto); 
        if (success)
        {
            StatusMessage = "Sanction created successfully.";
            CreateSanctionInput = new SanctionViewModel(); 
        }
        else
        {
            StatusMessage = "Error: Failed to create sanction.";
        }
        await LoadSanctionsAsync(); 
        return Page();
    }
}