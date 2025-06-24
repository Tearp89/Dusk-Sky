using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // Para [ValidateNever]
using System.Text.Json; // Para JsonSerializer


[Authorize(Roles = "admin,moderator")]
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


    public AddGameViewModel AddGameResult { get; set; } = new();
    public AddGameViewModel addGameInput { get; set; } = new();
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
        if (TempData.ContainsKey("StatusMessage"))
        {
            StatusMessage = TempData["StatusMessage"] as string ?? string.Empty;
        }

        // Recuperar el resultado de la importación de TempData si existe
        if (TempData.ContainsKey("AddGameResult"))
        {
            AddGameResult = System.Text.Json.JsonSerializer.Deserialize<AddGameViewModel>(TempData["AddGameResult"]!.ToString()!) ?? new();
        }
        else
        {
            AddGameResult = new AddGameViewModel(); // Asegúrate de que siempre esté inicializado
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin"))
        {
            await LoadAllUsersAsync();
        }
        return Page();
    }

    // --- Métodos de carga de datos ---

    private async Task LoadAllUsersAsync()
    {
        var allAuthUsers = await _authService.SearchUsersAsync("a"); // HACK para GetAllUsers

        if (allAuthUsers != null)
        {
            var userTasks = allAuthUsers.Select(async u =>
            {
                var profile = await _userManagerService.GetProfileAsync(u.Id);
                return new UserRoleViewModel
                {
                    UserId = u.Id,
                    Username = u.Username,
                    Role = u.Role,
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                };
            });
            AllUsers = (await Task.WhenAll(userTasks)).ToList();
        }
        else
        {
            AllUsers = new List<UserRoleViewModel>();
        }
    }

    private async Task LoadReportsAsync()
    {
        var reports = await _reportService.GetAllAsync(); // Asumo que _reportService.GetAllAsync() devuelve ReportDTO
        if (reports != null)
        {
            var displayTasks = reports.Select(async r =>
            {
                // Obtener el nombre de usuario del usuario reportado
                var reportedUserAccount = await _authService.SearchUserByIdAsync(r.ReportedUserId);
                var reportedUsername = reportedUserAccount?.Username ?? "Unknown User";

                // (Opcional) Obtener el nombre de usuario del reportero si ReportDTO tiene ReporterId
                // Tu SanctionDTO no lo tiene, así que esto es una suposición para ReportDTO.
                // Si tu ReportDTO tiene ReporterId, descomenta y usa:
                // var reporterUserAccount = await _authService.SearchUserByIdAsync(r.ReporterId);
                // var reporterUsername = reporterUserAccount?.Username ?? "Unknown User";

                return new ReportDisplayViewModel
                {
                    ReportId = r.Id,
                    ReportedUserId = r.ReportedUserId,
                    ReportedUsername = reportedUsername,
                    // ReporterId = r.ReporterId, // Descomentar si ReportDTO tiene
                    // ReporterUsername = reporterUsername, // Descomentar si ReportDTO tiene
                    ContentType = r.ContentType,
                    Reason = r.Reason,
                    Status = r.Status,
                    ReportedAt = r.ReportedAt
                };
            });
            RecentReports = (await Task.WhenAll(displayTasks)).OrderByDescending(r => r.ReportedAt).ToList();
        }
        else
        {
            RecentReports = new List<ReportDisplayViewModel>();
        }
    }

    private async Task LoadSanctionsAsync()
    {
        var sanctions = await _sanctionService.GetAllAsync(); // Obtener todas las sanciones
        if (sanctions != null)
        {
            var displayTasks = sanctions.Select(async s =>
            {
                var sanctionedUser = await _authService.SearchUserByIdAsync(s.UserId);

                // Usar el método de extensión IsActive
                bool isActive = s.IsActive();

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


    public async Task<JsonResult> OnGetSearchUsersForRolesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return new JsonResult(new List<UserRoleViewModel>());
        }

        try
        {
            var authUsers = await _authService.SearchUsersAsync(query);

            if (authUsers == null || !authUsers.Any())
            {
                return new JsonResult(new List<UserRoleViewModel>());
            }

            var userViewModels = new List<UserRoleViewModel>();
            foreach (var user in authUsers)
            {
                var profile = await _userManagerService.GetProfileAsync(user.Id);
                userViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                });
            }

            return new JsonResult(userViewModels);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error searching users: {ex.Message}");
            return new JsonResult(new { success = false, message = "Error interno al buscar usuarios." }) { StatusCode = 500 };
        }
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostPrepareSanctionAsync(string reportId, string reportedUserId)
    {
        // 1. Marcar el reporte como resuelto (lógica del antiguo OnPostResolveReportAsync)
        var report = await _reportService.GetByIdAsync(reportId);
        if (report != null)
        {
            report.Status = "resolved";
            await _reportService.UpdateAsync(reportId, report);
            StatusMessage = $"Reporte {reportId.Substring(0, 8)}... marcado como resuelto. Proceda con la sanción.";
        }

        // 2. Preparar el ViewModel para el modal de sanción
        CreateSanctionInput = new SanctionViewModel
        {
            ReportId = reportId,
            UserId = reportedUserId.Trim(),
            StartDate = DateTime.UtcNow // Pre-llenar con la fecha actual
        };

        // 3. Cargar los datos necesarios para la página
        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin"))
        {
            await LoadAllUsersAsync();
        }

        // 4. Añadir una bandera para que JavaScript sepa que debe abrir el modal
        ViewData["OpenCreateSanctionModal"] = true;

        // 5. Devolver la página para que se recargue con el estado actualizado
        return Page();
    }


    // --- Métodos OnPost (Acciones de Admin/Moderador) ---

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostAddGameAsync([FromForm] AddGameViewModel addGameInput)
    {
        if (!ModelState.IsValid)
        {
            // Conservar los datos del formulario para rellenar el modal
            AddGameResult = addGameInput;
            await LoadReportsAsync();
            await LoadSanctionsAsync();
            if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
            TempData["AddGameResult"] = JsonSerializer.Serialize(AddGameResult);
            TempData["StatusMessage"] = "Error: Por favor, corrija los errores del formulario.";
            return Page();
        }

        var regex = new Regex(@"https?:\/\/(?:store|steamcommunity)\.steampowered\.com\/(?:app|sub|bundle)\/(\d+)(?:[/?#].*)?$", RegexOptions.IgnoreCase);
        var match = regex.Match(addGameInput.SteamLink);

        int steamAppId;
        if (match.Success && int.TryParse(match.Groups[1].Value, out steamAppId))
        {
            // ID extraído con éxito, proceder con la importación
        }
        else
        {
            AddGameResult = addGameInput; // Conservar los datos
            AddGameResult.ImportStatusMessage = "Error: No se pudo encontrar un ID de aplicación de Steam válido en el enlace proporcionado. Asegúrese de que sea un enlace directo a una página de aplicación de Steam.";
            StatusMessage = AddGameResult.ImportStatusMessage;
            TempData["AddGameResult"] = JsonSerializer.Serialize(AddGameResult);
            TempData["StatusMessage"] = StatusMessage;
            return Page();
        }

        var (success, message, gameId) = await _gameService.ImportGameAsync(steamAppId);

        AddGameResult = addGameInput;
        if (success)
        {
            StatusMessage = $"Éxito: ¡Juego importado! {message}";
            AddGameResult.ImportStatusMessage = $"Éxito: {message}";
            AddGameResult.ImportedGameTitle = "Juego Importado";
            AddGameResult.ImportedGameId = gameId;
        }
        else
        {
            StatusMessage = $"Error: {message}";
            AddGameResult.ImportStatusMessage = $"Error: {message}";
        }

        TempData["AddGameResult"] = JsonSerializer.Serialize(AddGameResult);
        TempData["StatusMessage"] = StatusMessage;

        return RedirectToPage();
    }


    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostPromoteUserAsync([FromForm] PromoteUserInput input)
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Error: ID de usuario a promover es requerido.";
            await LoadReportsAsync();
            await LoadSanctionsAsync();
            if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
            return Page();
        }

        var userToPromote = await _authService.SearchUserByIdAsync(input.UserId);
        if (userToPromote == null)
        {
            StatusMessage = $"Error: Usuario {input.UserId} no encontrado.";
            return Page();
        }

        var success = await _authService.PromoteUserAsync(input.UserId);

        if (success)
        {
            StatusMessage = $"Usuario {userToPromote.Username} promovido a Moderador.";
        }
        else
        {
            StatusMessage = $"Error: Falló la promoción del usuario {userToPromote.Username}.";
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }

        return Page();
    }


    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostDemoteUserAsync([FromForm] DemoteUserInput input)
    {
        if (!ModelState.IsValid)
        {
            StatusMessage = "Error: ID de usuario a degradar es requerido.";
            await LoadReportsAsync();
            await LoadSanctionsAsync();
            if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
            return Page();
        }

        var userToDemote = await _authService.SearchUserByIdAsync(input.UserId);
        if (userToDemote == null)
        {
            StatusMessage = $"Error: Usuario {input.UserId} no encontrado.";
            return Page();
        }

        var success = await _authService.DemoteUserAsync(input.UserId);

        if (success)
        {
            StatusMessage = $"Usuario {userToDemote.Username} degradado de Moderador.";
        }
        else
        {
            StatusMessage = $"Error: Falló la degradación del usuario {userToDemote.Username}.";
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }

        return Page();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostResolveReportAsync(string reportId, string reportedUserId, string newStatus)
    {
        var report = await _reportService.GetByIdAsync(reportId);
        if (report == null)
        {
            return new JsonResult(new { success = false, message = "Reporte no encontrado." });
        }
        report.Status = newStatus;
        var success = await _reportService.UpdateAsync(reportId, report);
        if (success)
        {
            return new JsonResult(new { success = true, message = $"Reporte {reportId} marcado como {newStatus}.", reportedUserId = reportedUserId });
        }
        else
        {
            return new JsonResult(new { success = false, message = "Falló la actualización del estado del reporte." });
        }
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostDeleteReportAsync(string reportId)
    {
        if (string.IsNullOrEmpty(reportId))
        {
            TempData["StatusMessage"] = "Error: Se requiere el ID del reporte para eliminarlo.";
            return RedirectToPage();
        }

        var success = await _reportService.DeleteAsync(reportId);
        if (success)
        {
            // Usamos TempData para que el mensaje sobreviva a la redirección
            TempData["StatusMessage"] = $"Reporte #{reportId.Substring(0, 8)}... fue eliminado exitosamente.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: Falló la eliminación del reporte. Es posible que esté asociado a una sanción.";
        }

        // Redirigimos a la misma página para ver el resultado
        return RedirectToPage();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostCreateSanctionAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadReportsAsync();
            await LoadSanctionsAsync();
            if (User.IsInRole("admin")) await LoadAllUsersAsync();
            ViewData["OpenCreateSanctionModal"] = true;
            return Page();
        }

        // Verificar sanciones existentes
        var sanctions = await _sanctionService.GetAllAsync();

        var now = DateTime.UtcNow;

        var hasConflict = sanctions.Any(s =>
            s.UserId.Trim() == CreateSanctionInput.UserId.Trim() &&
            (
                // Ban permanente
                s.Type == SanctionType.ban ||

                // Superposición de suspensión: [start, end] intersects with [existing.Start, existing.End]
                (s.Type == SanctionType.suspension &&
                 CreateSanctionInput.StartDate < s.EndDate &&
                 CreateSanctionInput.EndDate > s.StartDate)
            )
        );

        if (hasConflict)
        {
            ModelState.AddModelError(string.Empty, "The user already has an active or overlapping sanction.");
            await LoadReportsAsync();
            await LoadSanctionsAsync();
            if (User.IsInRole("admin")) await LoadAllUsersAsync();
            ViewData["OpenCreateSanctionModal"] = true;
            return Page();
        }

        var sanctionDto = new SanctionDTO
        {
            ReportId = CreateSanctionInput.ReportId,
            UserId = CreateSanctionInput.UserId.Trim(),
            Type = CreateSanctionInput.Type,
            Reason = CreateSanctionInput.Reason,
            StartDate = DateTime.SpecifyKind(CreateSanctionInput.StartDate, DateTimeKind.Utc),
            EndDate = CreateSanctionInput.EndDate.HasValue
                        ? DateTime.SpecifyKind(CreateSanctionInput.EndDate.Value, DateTimeKind.Utc)
                        : (DateTime?)null
        };

        var success = await _sanctionService.CreateAsync(sanctionDto);
        if (success)
        {
            TempData["StatusMessage"] = "Sanción creada exitosamente.";
        }
        else
        {
            TempData["StatusMessage"] = "Error: Falló la creación de la sanción.";
        }

        return RedirectToPage();
    }


    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostApplySanctionAsync([FromForm] SanctionViewModel CreateSanctionInput, string reportId)
    {
        var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // 1. Crear sanción
        var sanctionDto = new SanctionDTO
        {
            ReportId = reportId, // Esto es ReportId, no Id de la sanción
            UserId = CreateSanctionInput.UserId,
            Reason = CreateSanctionInput.Reason,
            Type = CreateSanctionInput.Type,
            StartDate = DateTime.UtcNow,
            // Aquí el EndDate debe ser manejado. Si "type" es "ban", EndDate debe ser null.
            // Si "type" es "suspension", necesitas un EndDate.
            // Esta lógica es crucial y debería pasarse desde el cliente o definirse aquí.
            EndDate = (CreateSanctionInput.Type == SanctionType.suspension && CreateSanctionInput.EndDate.HasValue)
          ? CreateSanctionInput.EndDate
          : null

        };

        var sanctionCreated = await _sanctionService.CreateAsync(sanctionDto);

        // 2. Cambiar estado del reporte a "resolved"
        var report = await _reportService.GetByIdAsync(reportId);
        if (report != null)
        {
            report.Status = "resolved";
            await _reportService.UpdateAsync(reportId, report);
        }

        // 3. Mensajes de estado
        if (sanctionCreated)
        {
            StatusMessage = "Sanción aplicada correctamente y reporte resuelto.";
        }
        else
        {
            StatusMessage = "Error al aplicar la sanción.";
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        return RedirectToPage();
    }
}
