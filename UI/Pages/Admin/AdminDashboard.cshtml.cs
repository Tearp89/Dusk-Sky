using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; 
using System.Text.Json; 
using Microsoft.Extensions.Logging; 
using System.Net.Http; 

[Authorize(Roles = "admin,moderator")]
public class AdminDashboardModel : PageModel
{
    private readonly IAuthService _authService;
    private readonly IGameService _gameService;
    private readonly IModerationReportService _reportService;
    private readonly IModerationSanctionService _sanctionService;
    private readonly IUserManagerService _userManagerService;
    private readonly ILogger<AdminDashboardModel> _logger; 

    public List<UserRoleViewModel> AllUsers { get; set; } = new();
    public List<ReportDisplayViewModel> RecentReports { get; set; } = new();
    public List<SanctionViewModel> ActiveSanctions { get; set; } = new();

    public AddGameViewModel AddGameResult { get; set; } = new();
    public AddGameViewModel AddGameInput { get; set; } = new();
    [BindProperty]
    public SanctionViewModel CreateSanctionInput { get; set; } = new();

    [TempData]
    public string StatusMessage { get; set; } = string.Empty;
    [TempData]
    public string? SelectedSanctionUserJson { get; set; }
    [TempData]
    public bool ShouldOpenCreateSanctionModal { get; set; }
    [TempData]
    public string? CreateSanctionInputJson { get; set; }

    public UserRoleViewModel? SelectedSanctionUser { get; set; }

    public AdminDashboardModel(
        IAuthService authService,
        IGameService gameService,
        IModerationReportService reportService,
        IModerationSanctionService sanctionService,
        IUserManagerService userManagerService,
        ILogger<AdminDashboardModel> logger) 
    {
        _authService = authService;
        _gameService = gameService;
        _reportService = reportService;
        _sanctionService = sanctionService;
        _userManagerService = userManagerService;
        _logger = logger; 
    }

    public async Task<IActionResult> OnGetAsync()
    {
        _logger.LogInformation("Accediendo al Panel de Administración (OnGetAsync).");

        if (TempData.ContainsKey("StatusMessage"))
        {
            StatusMessage = TempData["StatusMessage"] as string ?? string.Empty;
            _logger.LogInformation("StatusMessage recuperado de TempData: {StatusMessage}", StatusMessage);
        }

        if (TempData.ContainsKey("AddGameResult"))
        {
            try
            {
                AddGameResult = JsonSerializer.Deserialize<AddGameViewModel>(TempData["AddGameResult"]!.ToString()!) ?? new();
                _logger.LogDebug("AddGameResult recuperado y deserializado de TempData.");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización al recuperar AddGameResult de TempData.");
                AddGameResult = new AddGameViewModel(); 
                StatusMessage = "Error interno: Problema al cargar datos del formulario de juego. Inténtalo de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al recuperar AddGameResult de TempData.");
                AddGameResult = new AddGameViewModel(); 
                StatusMessage = "Error interno: Problema inesperado al cargar datos del formulario de juego.";
            }
        }
        else
        {
            AddGameResult = new AddGameViewModel(); 
        }

        if (!string.IsNullOrEmpty(CreateSanctionInputJson))
        {
            try
            {
                CreateSanctionInput = JsonSerializer.Deserialize<SanctionViewModel>(CreateSanctionInputJson) ?? new();
                _logger.LogDebug("CreateSanctionInput recuperado y deserializado de TempData.");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización al recuperar CreateSanctionInput de TempData.");
                CreateSanctionInput = new SanctionViewModel(); 
                StatusMessage = "Error interno: Problema al cargar datos de sanción. Inténtalo de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al recuperar CreateSanctionInput de TempData.");
                CreateSanctionInput = new SanctionViewModel(); 
                StatusMessage = "Error interno: Problema inesperado al cargar datos de sanción.";
            }
        }

        if (!string.IsNullOrEmpty(SelectedSanctionUserJson))
        {
            try
            {
                SelectedSanctionUser = JsonSerializer.Deserialize<UserRoleViewModel>(SelectedSanctionUserJson);
                _logger.LogDebug("SelectedSanctionUser recuperado y deserializado de TempData.");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización al recuperar SelectedSanctionUser de TempData.");
                SelectedSanctionUser = null; 
                StatusMessage = "Error interno: Problema al cargar datos del usuario para sancionar. Inténtalo de nuevo.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al recuperar SelectedSanctionUser de TempData.");
                SelectedSanctionUser = null; 
                StatusMessage = "Error interno: Problema inesperado al cargar datos del usuario para sancionar.";
            }
        }

        
        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin"))
        {
            await LoadAllUsersAsync();
        }

        _logger.LogInformation("OnGetAsync completado.");
        return Page();
    }

    

    [Authorize(Roles = "admin,moderator")]
    public async Task<JsonResult> OnGetSearchUsersForSanctionAsync(string query)
    {
        _logger.LogInformation("Buscando usuarios para sanción con query: '{Query}'.", query);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            _logger.LogInformation("Query de búsqueda de usuario es nula/vacía o muy corta. Retornando lista vacía.");
            return new JsonResult(new List<UserRoleViewModel>());
        }

        try
        {
            var authUsers = await _authService.SearchUsersAsync(query);

            if (authUsers == null || !authUsers.Any())
            {
                _logger.LogInformation("No se encontraron usuarios en AuthService para la query: '{Query}'.", query);
                return new JsonResult(new List<UserRoleViewModel>());
            }

            var userViewModels = new List<UserRoleViewModel>();
            foreach (var user in authUsers)
            {
                if (string.IsNullOrEmpty(user.Id))
                {
                    _logger.LogWarning("Usuario con ID nulo o vacío encontrado al buscar perfiles para sanción. Saltando.");
                    continue; 
                }

                UserProfileDTO? profile = null;
                try
                {
                    profile = await _userManagerService.GetProfileAsync(user.Id);
                    if (profile == null)
                    {
                        _logger.LogWarning("Perfil de usuario nulo para el ID: {UserId} al buscar usuarios para sanción. Usando avatar por defecto.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener perfil para el usuario ID: {UserId} al buscar usuarios para sanción.", user.Id);
                }

                userViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                });
            }

            _logger.LogInformation("Se encontraron {Count} usuarios para sanción con query: '{Query}'.", userViewModels.Count, query);
            return new JsonResult(userViewModels);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al buscar usuarios para sanción con query: '{Query}'.", query);
            return new JsonResult(new { success = false, message = "Error de conexión al buscar usuarios." }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al buscar usuarios para sanción con query: '{Query}'.", query);
            return new JsonResult(new { success = false, message = "Error interno al buscar usuarios." }) { StatusCode = 500 };
        }
    }

    private async Task LoadAllUsersAsync()
    {
        _logger.LogInformation("Cargando todos los usuarios (solo Admin).");
        try
        {
            var allAuthUsers = await _authService.SearchUsersAsync("a");

            if (allAuthUsers != null)
            {
                var userTasks = allAuthUsers.Select(async u =>
                {
                    if (string.IsNullOrEmpty(u.Id))
                    {
                        _logger.LogWarning("Usuario con ID nulo o vacío encontrado al cargar todos los usuarios. Saltando.");
                        return null; 
                    }
                    UserProfileDTO? profile = null;
                    try
                    {
                        profile = await _userManagerService.GetProfileAsync(u.Id);
                        if (profile == null)
                        {
                            _logger.LogWarning("Perfil de usuario nulo para el ID: {UserId} al cargar todos los usuarios. Usando avatar por defecto.", u.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener perfil para el usuario ID: {UserId} al cargar todos los usuarios.", u.Id);
                    }

                    return new UserRoleViewModel
                    {
                        UserId = u.Id,
                        Username = u.Username,
                        Role = u.Role,
                        AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                    };
                }).Where(u => u != null); 

                AllUsers = (await Task.WhenAll(userTasks)).ToList()!; 
                _logger.LogInformation("Se cargaron {Count} usuarios para el dashboard de Admin.", AllUsers.Count);
            }
            else
            {
                _logger.LogWarning("AuthService.SearchUsersAsync('a') devolvió null. Inicializando AllUsers como lista vacía.");
                AllUsers = new List<UserRoleViewModel>();
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al cargar todos los usuarios.");
            StatusMessage = "Error de conexión al cargar la lista de usuarios. Inténtalo de nuevo más tarde.";
            AllUsers = new List<UserRoleViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cargar todos los usuarios.");
            StatusMessage = "Error interno al cargar la lista de usuarios. Contacta a soporte.";
            AllUsers = new List<UserRoleViewModel>();
        }
    }

    private async Task LoadReportsAsync()
    {
        _logger.LogInformation("Cargando reportes recientes.");
        try
        {
            var reports = await _reportService.GetAllAsync(); 
            if (reports != null)
            {
                var displayTasks = reports.Select(async r =>
                {
                    if (string.IsNullOrEmpty(r.ReportedUserId))
                    {
                        _logger.LogWarning("Reporte con ReportedUserId nulo/vacío encontrado (ReportId: {ReportId}). Saltando procesamiento.", r.Id);
                        return null; 
                    }

                    string reportedUsername = "Unknown User";
                    try
                    {
                        var reportedUserAccount = await _authService.SearchUserByIdAsync(r.ReportedUserId);
                        reportedUsername = reportedUserAccount?.Username ?? "Unknown User";
                        if (reportedUserAccount == null)
                        {
                            _logger.LogWarning("Usuario reportado no encontrado para ID: {ReportedUserId} (ReportId: {ReportId}).", r.ReportedUserId, r.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener el nombre de usuario del usuario reportado para ReportId: {ReportId}.", r.Id);
                    }

                   

                    return new ReportDisplayViewModel
                    {
                        ReportId = r.Id,
                        ReportedUserId = r.ReportedUserId,
                        ReportedUsername = reportedUsername,
                        
                        ContentType = r.ContentType,
                        Reason = r.Reason,
                        Status = r.Status,
                        ReportedAt = r.ReportedAt
                    };
                }).Where(r => r != null); 

                RecentReports = (await Task.WhenAll(displayTasks)).OrderByDescending(r => r!.ReportedAt).ToList()!;
                _logger.LogInformation("Se cargaron {Count} reportes.", RecentReports.Count);
            }
            else
            {
                _logger.LogWarning("ReportService.GetAllAsync devolvió null. Inicializando RecentReports como lista vacía.");
                RecentReports = new List<ReportDisplayViewModel>();
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al cargar reportes.");
            StatusMessage = "Error de conexión al cargar reportes. Inténtalo de nuevo más tarde.";
            RecentReports = new List<ReportDisplayViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cargar reportes.");
            StatusMessage = "Error interno al cargar reportes. Contacta a soporte.";
            RecentReports = new List<ReportDisplayViewModel>();
        }
    }

    private async Task LoadSanctionsAsync()
    {
        _logger.LogInformation("Cargando sanciones activas.");
        try
        {
            var sanctions = await _sanctionService.GetAllAsync(); 
            if (sanctions != null)
            {
                var displayTasks = sanctions.Select(async s =>
                {
                    if (string.IsNullOrEmpty(s.UserId))
                    {
                        _logger.LogWarning("Sanción con UserId nulo/vacío encontrada (SanctionId: {SanctionId}). Saltando procesamiento.", s.Id);
                        return null; 
                    }

                    UserSearchResultDto? sanctionedUserAccount = null;
                    try
                    {
                        sanctionedUserAccount = await _authService.SearchUserByIdAsync(s.UserId);
                        if (sanctionedUserAccount == null)
                        {
                            _logger.LogWarning("Usuario sancionado no encontrado para ID: {SanctionedUserId} (SanctionId: {SanctionId}).", s.UserId, s.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al obtener información de usuario sancionado para SanctionId: {SanctionId}.", s.Id);
                    }

                    bool isActive = s.IsActive();

                    return new SanctionViewModel
                    {
                        Id = s.Id,
                        UserId = s.UserId,
                        Username = sanctionedUserAccount?.Username ?? "Usuario desconocido", // Display username
                        Type = s.Type,
                        Reason = s.Reason,
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        IsActive = isActive
                    };
                }).Where(s => s != null); 

                ActiveSanctions = (await Task.WhenAll(displayTasks)).OrderByDescending(s => s!.StartDate).ToList()!;
                _logger.LogInformation("Se cargaron {Count} sanciones.", ActiveSanctions.Count);
            }
            else
            {
                _logger.LogWarning("SanctionService.GetAllAsync devolvió null. Inicializando ActiveSanctions como lista vacía.");
                ActiveSanctions = new List<SanctionViewModel>();
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al cargar sanciones.");
            StatusMessage = "Error de conexión al cargar sanciones. Inténtalo de nuevo más tarde.";
            ActiveSanctions = new List<SanctionViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al cargar sanciones.");
            StatusMessage = "Error interno al cargar sanciones. Contacta a soporte.";
            ActiveSanctions = new List<SanctionViewModel>();
        }
    }


    public async Task<JsonResult> OnGetSearchUsersForRolesAsync(string query)
    {
        _logger.LogInformation("Buscando usuarios para roles con query: '{Query}'.", query);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            _logger.LogInformation("Query de búsqueda de usuario para roles es nula/vacía o muy corta. Retornando lista vacía.");
            return new JsonResult(new List<UserRoleViewModel>());
        }

        try
        {
            var authUsers = await _authService.SearchUsersAsync(query);

            if (authUsers == null || !authUsers.Any())
            {
                _logger.LogInformation("No se encontraron usuarios en AuthService para roles con query: '{Query}'.", query);
                return new JsonResult(new List<UserRoleViewModel>());
            }

            var userViewModels = new List<UserRoleViewModel>();
            foreach (var user in authUsers)
            {
                if (string.IsNullOrEmpty(user.Id))
                {
                    _logger.LogWarning("Usuario con ID nulo o vacío encontrado al buscar perfiles para roles. Saltando.");
                    continue; 
                }
                UserProfileDTO? profile = null;
                try
                {
                    profile = await _userManagerService.GetProfileAsync(user.Id);
                    if (profile == null)
                    {
                        _logger.LogWarning("Perfil de usuario nulo para el ID: {UserId} al buscar usuarios para roles. Usando avatar por defecto.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener perfil para el usuario ID: {UserId} al buscar usuarios para roles.", user.Id);
                }

                userViewModels.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                });
            }

            _logger.LogInformation("Se encontraron {Count} usuarios para roles con query: '{Query}'.", userViewModels.Count, query);
            return new JsonResult(userViewModels);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al buscar usuarios para roles con query: '{Query}'.", query);
            return new JsonResult(new { success = false, message = "Error de conexión al buscar usuarios." }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al buscar usuarios para roles con query: '{Query}'.", query);
            return new JsonResult(new { success = false, message = "Error interno al buscar usuarios." }) { StatusCode = 500 };
        }
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostPrepareSanctionAsync(string reportId, string reportedUserId)
    {
        _logger.LogInformation("Preparando sanción para ReportId: {ReportId}, ReportedUserId: {ReportedUserId}.", reportId, reportedUserId);

        // Input validation
        if (string.IsNullOrEmpty(reportId) || string.IsNullOrEmpty(reportedUserId))
        {
            _logger.LogWarning("OnPostPrepareSanctionAsync: reportId o reportedUserId son nulos/vacíos.");
            StatusMessage = "Error: Faltan datos para preparar la sanción.";
            return RedirectToPage();
        }

        try
        {
            var report = await _reportService.GetByIdAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Reporte no encontrado con ID: {ReportId} al preparar sanción.", reportId);
                StatusMessage = "Error: El reporte especificado no fue encontrado.";
                return RedirectToPage();
            }

            // Update report status
            report.Status = "resolved";
            var updateReportSuccess = await _reportService.UpdateAsync(reportId, report);
            if (updateReportSuccess)
            {
                _logger.LogInformation("Reporte {ReportId} marcado como resuelto.", reportId);
                StatusMessage = $"Reporte {reportId.Substring(0, 8)}... marcado como resuelto. Proceda con la sanción.";
            }
            else
            {
                _logger.LogWarning("Falló la actualización del estado del reporte {ReportId} a 'resolved'.", reportId);
                StatusMessage = $"Advertencia: Falló la actualización del estado del reporte {reportId.Substring(0, 8)}....";
            }

            var sanctionInput = new SanctionViewModel
            {
                ReportId = reportId,
                UserId = reportedUserId.Trim(),
                StartDate = DateTime.UtcNow
            };
            CreateSanctionInput = sanctionInput; 

            CreateSanctionInputJson = JsonSerializer.Serialize(sanctionInput);
            _logger.LogDebug("CreateSanctionInputJson serializado y guardado en TempData.");

            UserSearchResultDto? user = null;
            try
            {
                user = await _authService.SearchUserByIdAsync(reportedUserId);
                if (user == null)
                {
                    _logger.LogWarning("Usuario reportado no encontrado por ID: {ReportedUserId} al preparar sanción.", reportedUserId);
                    StatusMessage += " Sin embargo, el usuario reportado no pudo ser cargado.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar usuario reportado por ID: {ReportedUserId} al preparar sanción.", reportedUserId);
                StatusMessage += " Sin embargo, hubo un error al cargar el usuario reportado.";
            }


            UserRoleViewModel? selectedUserForDisplay = null;
            if (user != null)
            {
                UserProfileDTO? profile = null;
                try
                {
                    profile = await _userManagerService.GetProfileAsync(user.Id);
                    if (profile == null)
                    {
                        _logger.LogWarning("Perfil de usuario nulo para el ID: {UserId} al preparar sanción. Usando avatar por defecto.", user.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener perfil para el usuario ID: {UserId} al preparar sanción.", user.Id);
                }

                selectedUserForDisplay = new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png"
                };
            }
            SelectedSanctionUserJson = JsonSerializer.Serialize(selectedUserForDisplay);
            _logger.LogDebug("SelectedSanctionUserJson serializado y guardado en TempData.");

            ShouldOpenCreateSanctionModal = true;
            _logger.LogInformation("Bandera ShouldOpenCreateSanctionModal activada.");
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al preparar sanción para ReportId: {ReportId}.", reportId);
            StatusMessage = "Error de conexión al preparar la sanción. Inténtalo de nuevo más tarde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al preparar sanción para ReportId: {ReportId}.", reportId);
            StatusMessage = "Error interno al preparar la sanción. Contacta a soporte.";
        }

        return RedirectToPage();
    }



    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostAddGameAsync([FromForm] AddGameViewModel addGameInput)
    {
        _logger.LogInformation("Intento de añadir juego. SteamLink: {SteamLink}", addGameInput.SteamLink);

        if (TempData.ContainsKey("SelectedSanctionUserJson"))
        {
            SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string;
        }
        if (TempData.ContainsKey("CreateSanctionInputJson"))
        {
            CreateSanctionInputJson = TempData["CreateSanctionInputJson"] as string;
        }



        var regex = new Regex(@"https?:\/\/(?:store|steamcommunity)\.steampowered\.com\/(?:app|sub|bundle)\/(\d+)(?:[/?#].*)?$", RegexOptions.IgnoreCase);
        var match = regex.Match(addGameInput.SteamLink ?? string.Empty); // Ensure SteamLink is not null

        int steamAppId;
        if (match.Success && int.TryParse(match.Groups[1].Value, out steamAppId))
        {
            _logger.LogInformation("Steam App ID '{SteamAppId}' extraído del enlace: {SteamLink}", steamAppId, addGameInput.SteamLink);
            try
            {
                var (success, message, gameId) = await _gameService.ImportGameAsync(steamAppId);

                AddGameResult = addGameInput; 
                if (success)
                {
                    StatusMessage = $"Éxito: ¡Juego importado! {message}";
                    AddGameResult.ImportStatusMessage = $"Éxito: {message}";
                    AddGameResult.ImportedGameTitle = message; 
                    AddGameResult.ImportedGameId = gameId;
                    _logger.LogInformation("Juego importado exitosamente: {GameId} - {Message}", gameId, message);
                }
                else
                {
                    StatusMessage = $"Error: {message}";
                    AddGameResult.ImportStatusMessage = $"Error: {message}";
                    _logger.LogError("Error al importar juego con Steam App ID {SteamAppId}: {Message}", steamAppId, message);
                }
            }
            catch (HttpRequestException httpEx)
            {
                StatusMessage = "Error de conexión: Falló la importación del juego desde Steam. Inténtalo de nuevo más tarde.";
                AddGameResult = addGameInput;
                AddGameResult.ImportStatusMessage = "Error de conexión con el servicio de juegos.";
                _logger.LogError(httpEx, "Error de red al intentar importar juego con Steam App ID {SteamAppId}.", steamAppId);
            }
            catch (Exception ex)
            {
                StatusMessage = "Error inesperado al importar el juego.";
                AddGameResult = addGameInput;
                AddGameResult.ImportStatusMessage = "Error interno durante la importación del juego.";
                _logger.LogError(ex, "Error inesperado al importar juego con Steam App ID {SteamAppId}.", steamAppId);
            }
        }
        else
        {
            _logger.LogWarning("No se pudo extraer Steam App ID del enlace: {SteamLink}. Formato inválido.", addGameInput.SteamLink);
            AddGameResult = addGameInput; 
            AddGameResult.ImportStatusMessage = "Error: No se pudo encontrar un ID de aplicación de Steam válido en el enlace proporcionado. Asegúrese de que sea un enlace directo a una página de aplicación de Steam.";
            StatusMessage = AddGameResult.ImportStatusMessage;
        }

        TempData["AddGameResult"] = JsonSerializer.Serialize(AddGameResult);
        TempData["StatusMessage"] = StatusMessage;

        return RedirectToPage();
    }


    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostPromoteUserAsync([FromForm] PromoteUserInput promoteInput)
    {
        _logger.LogInformation("Intento de promover usuario con UserId: {UserId}.", promoteInput.UserId);

        if (TempData.ContainsKey("SelectedSanctionUserJson"))
        {
            SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string;
        }
        if (TempData.ContainsKey("CreateSanctionInputJson"))
        {
            CreateSanctionInputJson = TempData["CreateSanctionInputJson"] as string;
        }

        if (string.IsNullOrEmpty(promoteInput.UserId))
        {
            _logger.LogWarning("OnPostPromoteUserAsync: UserId a promover es nulo o vacío.");
            StatusMessage = "Error: ID de usuario a promover es requerido.";
            return Page();
        }

        UserSearchResultDto? userToPromote = null;
        try
        {
            userToPromote = await _authService.SearchUserByIdAsync(promoteInput.UserId);
            if (userToPromote == null)
            {
                _logger.LogWarning("Usuario {UserId} no encontrado para promoción.", promoteInput.UserId);
                StatusMessage = $"Error: Usuario {promoteInput.UserId} no encontrado.";
                await LoadReportsAsync();
                await LoadSanctionsAsync();
                if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario {UserId} para promoción.", promoteInput.UserId);
            StatusMessage = "Error interno al buscar el usuario. Contacta a soporte.";
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
            return Page();
        }

        try
        {
            var success = await _authService.PromoteUserAsync(promoteInput.UserId);

            if (success)
            {
                _logger.LogInformation("Usuario {Username} ({UserId}) promovido a Moderador exitosamente.", userToPromote.Username, promoteInput.UserId);
                StatusMessage = $"Usuario {userToPromote.Username} promovido a Moderador.";
            }
            else
            {
                _logger.LogWarning("Falló la promoción del usuario {Username} ({UserId}). El servicio indicó un problema.", userToPromote.Username, promoteInput.UserId);
                StatusMessage = $"Error: Falló la promoción del usuario {userToPromote.Username}.";
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar promover al usuario {UserId}.", promoteInput.UserId);
            StatusMessage = "Error de conexión al promover al usuario. Inténtalo de nuevo más tarde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar promover al usuario {UserId}.", promoteInput.UserId);
            StatusMessage = "Error interno al promover al usuario. Contacta a soporte.";
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }

        return Page();
    }


    [Authorize(Roles = "admin")]
    public async Task<IActionResult> OnPostDemoteUserAsync([FromForm] DemoteUserInput demoteInput)
    {
        _logger.LogInformation("Intento de degradar usuario con UserId: {UserId}.", demoteInput.UserId);

        if (TempData.ContainsKey("SelectedSanctionUserJson"))
        {
            SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string;
        }
        if (TempData.ContainsKey("CreateSanctionInputJson"))
        {
            CreateSanctionInputJson = TempData["CreateSanctionInputJson"] as string;
        }

        if (string.IsNullOrEmpty(demoteInput.UserId))
        {
            _logger.LogWarning("OnPostDemoteUserAsync: UserId a degradar es nulo o vacío.");
            StatusMessage = "Error: ID de usuario a degradar es requerido.";
            return Page();
        }

        UserSearchResultDto? userToDemote = null;
        try
        {
            userToDemote = await _authService.SearchUserByIdAsync(demoteInput.UserId);
            if (userToDemote == null)
            {
                _logger.LogWarning("Usuario {UserId} no encontrado para degradación.", demoteInput.UserId);
                StatusMessage = $"Error: Usuario {demoteInput.UserId} no encontrado.";
                await LoadReportsAsync();
                await LoadSanctionsAsync();
                if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar usuario {UserId} para degradación.", demoteInput.UserId);
            StatusMessage = "Error interno al buscar el usuario. Contacta a soporte.";
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
            return Page();
        }

        try
        {
            var success = await _authService.DemoteUserAsync(demoteInput.UserId);

            if (success)
            {
                _logger.LogInformation("Usuario {Username} ({UserId}) degradado de Moderador exitosamente.", userToDemote.Username, demoteInput.UserId);
                StatusMessage = $"Usuario {userToDemote.Username} degradado de Moderador.";
            }
            else
            {
                _logger.LogWarning("Falló la degradación del usuario {Username} ({UserId}). El servicio indicó un problema.", userToDemote.Username, demoteInput.UserId);
                StatusMessage = $"Error: Falló la degradación del usuario {userToDemote.Username}.";
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar degradar al usuario {UserId}.", demoteInput.UserId);
            StatusMessage = "Error de conexión al degradar al usuario. Inténtalo de nuevo más tarde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar degradar al usuario {UserId}.", demoteInput.UserId);
            StatusMessage = "Error interno al degradar al usuario. Contacta a soporte.";
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }

        return Page();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostResolveReportAsync(string reportId, string reportedUserId, string newStatus)
    {
        _logger.LogInformation("Intentando resolver reporte. ReportId: {ReportId}, ReportedUserId: {ReportedUserId}, NewStatus: {NewStatus}.", reportId, reportedUserId, newStatus);

        if (string.IsNullOrEmpty(reportId) || string.IsNullOrEmpty(reportedUserId) || string.IsNullOrEmpty(newStatus))
        {
            _logger.LogWarning("OnPostResolveReportAsync: Datos de entrada nulos o vacíos.");
            return new JsonResult(new { success = false, message = "Datos de entrada inválidos para resolver el reporte." });
        }

        try
        {
            var report = await _reportService.GetByIdAsync(reportId);
            if (report == null)
            {
                _logger.LogWarning("Reporte no encontrado con ID: {ReportId} al intentar resolver.", reportId);
                return new JsonResult(new { success = false, message = "Reporte no encontrado." });
            }

            report.Status = newStatus;
            var success = await _reportService.UpdateAsync(reportId, report);
            if (success)
            {
                _logger.LogInformation("Reporte {ReportId} marcado como {NewStatus} exitosamente.", reportId, newStatus);
                return new JsonResult(new { success = true, message = $"Reporte {reportId} marcado como {newStatus}.", reportedUserId = reportedUserId });
            }
            else
            {
                _logger.LogWarning("Falló la actualización del estado del reporte {ReportId} a {NewStatus}.", reportId, newStatus);
                return new JsonResult(new { success = false, message = "Falló la actualización del estado del reporte." });
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar resolver reporte {ReportId}.", reportId);
            return new JsonResult(new { success = false, message = "Error de conexión al resolver el reporte." }) { StatusCode = 500 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar resolver reporte {ReportId}.", reportId);
            return new JsonResult(new { success = false, message = "Error interno al resolver el reporte." }) { StatusCode = 500 };
        }
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostDeleteReportAsync(string reportId)
    {
        _logger.LogInformation("Intentando eliminar reporte con ReportId: {ReportId}.", reportId);

        if (string.IsNullOrEmpty(reportId))
        {
            _logger.LogWarning("OnPostDeleteReportAsync: ReportId es nulo o vacío.");
            TempData["StatusMessage"] = "Error: Se requiere el ID del reporte para eliminarlo.";
            return RedirectToPage();
        }

        try
        {
            var success = await _reportService.DeleteAsync(reportId);
            if (success)
            {
                _logger.LogInformation("Reporte #{ShortReportId}... fue eliminado exitosamente.", reportId.Substring(0, Math.Min(reportId.Length, 8)));
                TempData["StatusMessage"] = $"Reporte #{reportId.Substring(0, Math.Min(reportId.Length, 8))}... fue eliminado exitosamente.";
            }
            else
            {
                _logger.LogWarning("Falló la eliminación del reporte {ReportId}. Es posible que no exista o esté asociado a una sanción.", reportId);
                TempData["StatusMessage"] = "Error: Falló la eliminación del reporte. Es posible que esté asociado a una sanción o no exista.";
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar eliminar reporte {ReportId}.", reportId);
            TempData["StatusMessage"] = "Error de conexión al eliminar el reporte. Inténtalo de nuevo más tarde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar eliminar reporte {ReportId}.", reportId);
            TempData["StatusMessage"] = "Error interno al eliminar el reporte. Contacta a soporte.";
        }

        return RedirectToPage();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostCreateSanctionAsync()
    {
        _logger.LogInformation("Intentando crear sanción para UserId: {UserId}, Tipo: {Type}.", CreateSanctionInput.UserId, CreateSanctionInput.Type);

        if (!string.IsNullOrEmpty(SelectedSanctionUserJson))
        {
            try
            {
                SelectedSanctionUser = JsonSerializer.Deserialize<UserRoleViewModel>(SelectedSanctionUserJson);
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Error de deserialización al rehidratar SelectedSanctionUser para validación de sanción.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al rehidratar SelectedSanctionUser para validación de sanción.");
            }
        }

        if (string.IsNullOrEmpty(CreateSanctionInput.UserId) || string.IsNullOrEmpty(CreateSanctionInput.Reason) ) // Assuming SanctionType.None as default
        {
            _logger.LogWarning("OnPostCreateSanctionAsync: Datos de sanción incompletos. UserId: {UserId}, Reason Length: {ReasonLen}, Type: {Type}",
                CreateSanctionInput.UserId, CreateSanctionInput.Reason?.Length, CreateSanctionInput.Type);
            ModelState.AddModelError(string.Empty, "Por favor, completa todos los campos requeridos para la sanción.");
            ShouldOpenCreateSanctionModal = true; // Keep modal open
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
            return Page();
        }

        if (CreateSanctionInput.Type == SanctionType.suspension && !CreateSanctionInput.EndDate.HasValue)
        {
            _logger.LogWarning("OnPostCreateSanctionAsync: Sanción de tipo 'suspension' sin fecha de fin.");
            ModelState.AddModelError(nameof(CreateSanctionInput.EndDate), "La fecha de fin es requerida para una suspensión.");
            ShouldOpenCreateSanctionModal = true; // Keep modal open
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
            return Page();
        }

        try
        {
            var sanctions = await _sanctionService.GetAllAsync();
            if (sanctions == null)
            {
                _logger.LogWarning("SanctionService.GetAllAsync devolvió null al verificar conflictos de sanción. Asumiendo lista vacía.");
                sanctions = new List<SanctionDTO>();
            }

            var now = DateTime.UtcNow;

            var hasConflict = sanctions.Any(s =>
                s.UserId.Trim().Equals(CreateSanctionInput.UserId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (
                    s.Type == SanctionType.ban || // Ban conflicts with any other active or overlapping sanction
                    (s.Type == SanctionType.suspension &&
                     CreateSanctionInput.StartDate < s.EndDate && // New sanction start before old sanction ends
                     (CreateSanctionInput.EndDate == null || CreateSanctionInput.EndDate > s.StartDate)) // New sanction ends after old sanction starts (if new sanction has an end date)
                )
            );

            if (hasConflict)
            {
                _logger.LogWarning("Conflicto de sanción detectado para el usuario {UserId}. Ya existe una sanción activa o superpuesta.", CreateSanctionInput.UserId);
                ModelState.AddModelError(string.Empty, "El usuario ya tiene una sanción activa o superpuesta.");
                ShouldOpenCreateSanctionModal = true; // Keep modal open
                await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
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
                _logger.LogInformation("Sanción creada exitosamente para el usuario {UserId}, Tipo: {Type}.", CreateSanctionInput.UserId, CreateSanctionInput.Type);
                TempData["StatusMessage"] = "Sanción creada exitosamente.";
                TempData.Remove("SelectedSanctionUserJson"); // Clear selected user from TempData after successful creation
                TempData.Remove("CreateSanctionInputJson"); // Clear form input
            }
            else
            {
                _logger.LogWarning("Falló la creación de la sanción para el usuario {UserId}. El servicio indicó un problema.", CreateSanctionInput.UserId);
                TempData["StatusMessage"] = "Error: Falló la creación de la sanción.";
                ShouldOpenCreateSanctionModal = true; // If creation fails, keep modal open
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar crear sanción para el usuario {UserId}.", CreateSanctionInput.UserId);
            TempData["StatusMessage"] = "Error de conexión al crear la sanción. Inténtalo de nuevo más tarde.";
            ShouldOpenCreateSanctionModal = true; // Keep modal open on network error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar crear sanción para el usuario {UserId}.", CreateSanctionInput.UserId);
            TempData["StatusMessage"] = "Error interno al crear la sanción. Contacta a soporte.";
            ShouldOpenCreateSanctionModal = true; // Keep modal open on unexpected error
        }

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) await LoadAllUsersAsync();
        return RedirectToPage();
    }

    [Authorize(Roles = "admin,moderator")]
    public async Task<IActionResult> OnPostApplySanctionAsync([FromForm] SanctionViewModel CreateSanctionInput, string reportId)
    {
        _logger.LogInformation("Intento de aplicar sanción (desde modal de Reporte) para ReportId: {ReportId}, UserId: {UserId}, Tipo: {Type}.", reportId, CreateSanctionInput.UserId, CreateSanctionInput.Type);

        var moderatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(moderatorId))
        {
            _logger.LogWarning("OnPostApplySanctionAsync: ID de moderador no encontrado.");
            StatusMessage = "Error: Tu sesión de moderador no es válida. Por favor, vuelve a iniciar sesión.";
            return RedirectToPage();
        }

        if (string.IsNullOrEmpty(reportId) || string.IsNullOrEmpty(CreateSanctionInput.UserId) || string.IsNullOrEmpty(CreateSanctionInput.Reason))
        {
            _logger.LogWarning("OnPostApplySanctionAsync: Datos de entrada incompletos para aplicar sanción desde reporte. ReportId: {ReportId}, UserId: {UserId}, Reason Length: {ReasonLen}.",
                reportId, CreateSanctionInput.UserId, CreateSanctionInput.Reason?.Length);
            StatusMessage = "Error: Faltan datos para aplicar la sanción y resolver el reporte.";
            // Re-open modal if validation fails
            ShouldOpenCreateSanctionModal = true;
            SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string; // Restore for display
            CreateSanctionInputJson = JsonSerializer.Serialize(CreateSanctionInput); // Restore form data
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
            return Page();
        }

        // 1. Crear sanción
        SanctionDTO? sanctionDto = null;
        try
        {
            sanctionDto = new SanctionDTO
            {
                ReportId = reportId, 
                UserId = CreateSanctionInput.UserId,
                Reason = CreateSanctionInput.Reason,
                Type = CreateSanctionInput.Type,
                StartDate = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc), 
                
                EndDate = (CreateSanctionInput.Type == SanctionType.suspension && CreateSanctionInput.EndDate.HasValue)
                          ? DateTime.SpecifyKind(CreateSanctionInput.EndDate.Value, DateTimeKind.Utc)
                          : (DateTime?)null
            };
            
            var existingSanctions = await _sanctionService.GetAllAsync();
            if (existingSanctions != null && existingSanctions.Any(s => 
                s.UserId.Trim().Equals(sanctionDto.UserId.Trim(), StringComparison.OrdinalIgnoreCase) &&
                (s.Type == SanctionType.ban || (s.Type == SanctionType.suspension && sanctionDto.StartDate < s.EndDate && (sanctionDto.EndDate == null || sanctionDto.EndDate > s.StartDate)))))
            {
                _logger.LogWarning("Conflicto de sanción detectado al aplicar sanción desde reporte para el usuario {UserId}. Ya existe una sanción activa o superpuesta.", CreateSanctionInput.UserId);
                StatusMessage = "Error: El usuario ya tiene una sanción activa o superpuesta. No se puede aplicar esta sanción.";
                ShouldOpenCreateSanctionModal = true;
                SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string;
                CreateSanctionInputJson = JsonSerializer.Serialize(CreateSanctionInput);
                await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
                return Page();
            }

            var sanctionCreated = await _sanctionService.CreateAsync(sanctionDto);
            if (!sanctionCreated)
            {
                _logger.LogWarning("Falló la creación de la sanción para el reporte {ReportId}.", reportId);
                StatusMessage = "Error: Falló la creación de la sanción.";
                ShouldOpenCreateSanctionModal = true; // Keep modal open
                SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string;
                CreateSanctionInputJson = JsonSerializer.Serialize(CreateSanctionInput);
                await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
                return Page();
            }
            _logger.LogInformation("Sanción creada exitosamente para el reporte {ReportId}.", reportId);
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al crear sanción para reporte {ReportId}.", reportId);
            StatusMessage = "Error de conexión al crear la sanción. Inténtalo de nuevo más tarde.";
            ShouldOpenCreateSanctionModal = true; SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string; CreateSanctionInputJson = JsonSerializer.Serialize(CreateSanctionInput);
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear sanción para reporte {ReportId}.", reportId);
            StatusMessage = "Error interno al crear la sanción. Contacta a soporte.";
            ShouldOpenCreateSanctionModal = true; SelectedSanctionUserJson = TempData["SelectedSanctionUserJson"] as string; CreateSanctionInputJson = JsonSerializer.Serialize(CreateSanctionInput);
            await LoadReportsAsync(); await LoadSanctionsAsync(); if (User.IsInRole("admin")) await LoadAllUsersAsync();
            return Page();
        }

        ReportDTO? report = null;
        try
        {
            report = await _reportService.GetByIdAsync(reportId);
            if (report != null)
            {
                report.Status = "resolved";
                var updateReportSuccess = await _reportService.UpdateAsync(reportId, report);
                if (updateReportSuccess)
                {
                    _logger.LogInformation("Reporte {ReportId} marcado como resuelto exitosamente.", reportId);
                }
                else
                {
                    _logger.LogWarning("Falló la actualización del estado del reporte {ReportId} a 'resolved'.", reportId);
                    StatusMessage += " Sin embargo, el reporte no se pudo marcar como resuelto.";
                }
            }
            else
            {
                _logger.LogWarning("Reporte {ReportId} no encontrado al intentar marcar como resuelto después de sancionar.", reportId);
                StatusMessage += " Advertencia: El reporte original no fue encontrado para ser marcado como resuelto.";
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al intentar actualizar el estado del reporte {ReportId} después de sancionar.", reportId);
            StatusMessage += " Advertencia: Error de conexión al actualizar el reporte.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar actualizar el estado del reporte {ReportId} después de sancionar.", reportId);
            StatusMessage += " Advertencia: Error interno al actualizar el reporte.";
        }

        StatusMessage = "Sanción aplicada correctamente y reporte resuelto." + StatusMessage; // Combine messages
        
        TempData.Remove("SelectedSanctionUserJson");
        TempData.Remove("CreateSanctionInputJson");
        TempData.Remove("ShouldOpenCreateSanctionModal");

        await LoadReportsAsync();
        await LoadSanctionsAsync();
        if (User.IsInRole("admin")) { await LoadAllUsersAsync(); }
        return RedirectToPage();
    }
}

