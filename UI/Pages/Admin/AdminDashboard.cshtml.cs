using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Text.Json.Serialization; 

// Asegúrate de que los using apunten a tus servicios y ViewModels
// Por ejemplo:
// using YourApp.Services;
// using YourApp.ViewModels; 


// =========================================================
// DTOs y ViewModels específicos para este PageModel
// Puedes poner estos en un archivo AdminViewModels.cs
// o dentro de este mismo archivo como clases anidadas si prefieres.
// =========================================================



// DTOs de servicio (deben estar en tus archivos de DTOs globales, ej. DTOs/ReportDTO.cs)
// Asegúrate de que tus ReportDTO y SanctionDTO reales tengan JsonPropertyName
// para coincidir con tu base de datos y API de FastAPI (camelCase vs snake_case).
// Si ya los tienes en archivos separados, ELIMINA estas declaraciones de aquí.
/*
public class ReportDTO 
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("reporter_id")] public string ReporterId { get; set; } = string.Empty; 
    [JsonPropertyName("reported_user_id")] public string ReportedUserId { get; set; } = string.Empty;
    [JsonPropertyName("content_type")] public string ContentType { get; set; } = string.Empty; 
    [JsonPropertyName("reason")] public string Reason { get; set; } = string.Empty;
    [JsonPropertyName("reported_at")] public DateTime ReportedAt { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("resolved_at")] public DateTime? ResolvedAt { get; set; } 
}

public class SanctionDTO 
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
        // Esto asume que tu IAuthService tiene un método GetAllUsersAsync() que devuelve List<UserSearchResultDto>
        // Si no tienes GetAllUsersAsync en IAuthService, usaremos SearchUsersAsync con una query amplia.
        // O lo mejor es que tu backend tenga un endpoint /auth/users/all.

        // OPCIÓN A: Si tu IAuthService TIENE un GetAllUsersAsync() (que devolvería List<UserSearchResultDto>)
        // Implementarías esto:
        // var allAuthUsers = await _authService.GetAllUsersAsync();
        // if (allAuthUsers != null) {
        //     var userTasks = allAuthUsers.Select(async u => {
        //         var profile = await _userManagerService.GetProfileAsync(u.Id);
        //         return new UserRoleViewModel {
        //             UserId = u.Id,
        //             Username = u.Username,
        //             Role = u.Role, // Usar el campo Role singular
        //             AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
        //         };
        //     });
        //     AllUsers = (await Task.WhenAll(userTasks)).ToList();
        // }


        // OPCIÓN B: Usar SearchUsersAsync con una query muy amplia (si tu API lo soporta)
        // Por ejemplo, buscar un solo carácter que casi todos los nombres de usuario tendrán.
        // Esto es un HACK si no hay GetAllUsersAsync.
        var allAuthUsers = await _authService.SearchUsersAsync("a"); // Buscar por 'a' para traer muchos usuarios
        
        if (allAuthUsers != null)
        {
            var userTasks = allAuthUsers.Select(async u =>
            {
                var profile = await _userManagerService.GetProfileAsync(u.Id);
                return new UserRoleViewModel
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Role = u.Role, // Usar el campo Role singular de UserSearchResultDto
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                };
            });
            AllUsers = (await Task.WhenAll(userTasks)).ToList();
        } else {
            AllUsers = new List<UserRoleViewModel>(); // Asegurar que no sea null
        }
    }

    
    public async Task<JsonResult> OnGetSearchUsersForRolesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2) // Mínimo 2 caracteres para buscar
        {
            return new JsonResult(new List<UserRoleViewModel>());
        }

        // Llamar al servicio de autenticación para buscar usuarios por username
        var userResults = await _authService.SearchUsersAsync(query); // Asumo que devuelve List<UserSearchResultDto>

        var userRoleViewModels = new List<UserRoleViewModel>();
        foreach (var userDto in userResults)
        {
            // Obtener el perfil para la URL del avatar
            var profile = await _userManagerService.GetProfileAsync(userDto.Id);
            userRoleViewModels.Add(new UserRoleViewModel
            {
                UserId = userDto.Id,
                Username = userDto.Username,
                // ¡IMPORTANTE! Tu UserSearchResultDto (en Auth/Models) debe tener una propiedad 'Roles'.
                // Si no la tiene, tu backend FastAPI (en el endpoint /users/search o /users/{id})
                // debería devolver los roles. Si no, esta lista estará vacía.
                Role = userDto.Role , // Asumo userDto.Roles existe
                AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
            });
        }
        return new JsonResult(userRoleViewModels);
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
                // Si tu ReportDTO tiene un ReporterId, también búscalo.

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
                // Determinar si la sanción está activa (cálculo en frontend, si API no lo da)
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
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }

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
    public async Task<IActionResult> OnPostResolveReportAsync(string reportId, string reportedUserId, string newStatus)
    {
        var report = await _reportService.GetByIdAsync(reportId);
        if (report == null)
        {
            return new JsonResult(new { success = false, message = "Report not found." });
        }
        report.Status = newStatus; 
        var success = await _reportService.UpdateAsync(reportId, report);
        if (success)
        {
            // ¡IMPORTANTE! Devolver el reportedUserId para que el JS pueda pre-llenar el modal
            return new JsonResult(new { success = true, message = $"Report {reportId} marked as {newStatus}.", reportedUserId = reportedUserId });
        }
        else
        {
            return new JsonResult(new { success = false, message = "Failed to update report status." });
        }
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