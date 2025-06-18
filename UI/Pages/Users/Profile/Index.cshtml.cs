using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System; // Asegúrate de tener este using

// Asumo que la clase base ProfileModelBase y el ViewModel FriendViewModel ya están definidos

public class ProfileModel : ProfileModelBase // Hereda de la clase base
{
    // --- Servicios necesarios para esta página y para la clase base ---
    private readonly IAuthService _authService;
    private readonly IUserManagerService _userManagerService;
    private readonly IFriendshipService _friendshipService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _listService;
    private readonly IGameTrackingService _gameTrackingService; // <-- ¡Nuevo!

    // --- Propiedad para guardar los amigos de este usuario ---
    public List<FriendViewModel> Friends { get; set; } = new();

    // --- Nueva propiedad para los datos del calendario de actividad ---
    // Dictionary<Fecha, NumeroDeActividades>
    public Dictionary<DateTime, int> DailyActivityCounts { get; set; } = new();

    public ProfileModel(
        IAuthService authService,
        IUserManagerService userManagerService,
        IFriendshipService friendshipService,
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService) // <-- Inyectar GameTrackingService
    {
        _authService = authService;
        _userManagerService = userManagerService;
        _friendshipService = friendshipService;
        _reviewService = reviewService;
        _listService = listService;
        _gameTrackingService = gameTrackingService; // <-- Asignar
    }

    public async Task<IActionResult> OnGetAsync(string userId)
    {
        // 1. Le decimos a la barra de navegación que esta es la pestaña "Profile"
        ActiveTab = "Profile"; 
        
        // 2. Cargamos todos los datos del encabezado usando el método de la clase base
        var userExists = await LoadProfileHeaderData(
            userId, 
            _authService, 
            _userManagerService, 
            _friendshipService,
            _reviewService,
            _listService);
            
        if (!userExists)
        {
            return NotFound();
        }

        // 3. Obtenemos los datos específicos para ESTA PÁGINA:
        //    a) La lista de amigos (lo que ya tenías)
        var friendDocs = await _friendshipService.GetFriendsAsync(userId);

        if (friendDocs != null && friendDocs.Any())
        {
            var friendTasks = friendDocs.Select(async f =>
            {
                var friendId = f.SenderId == userId ? f.ReceiverId : f.SenderId;
                
                var friendAuthUser = await _authService.SearchUserByIdAsync(friendId);
                var friendProfile = await _userManagerService.GetProfileAsync(friendId);
                
                return new FriendViewModel
                {
                    UserId = friendId,
                    Username = friendAuthUser?.Username ?? "Unknown User",
                    AvatarUrl = friendProfile?.AvatarUrl ?? "/images/default-avatar.png"
                };
            });
            
            Friends = (await Task.WhenAll(friendTasks)).ToList();
        }

        //    b) Datos de actividad para el calendario
        await LoadActivityCalendarData(userId);

        return Page();
    }

    private async Task LoadActivityCalendarData(string userId)
    {
        // Definir el rango de fechas (ej. el último año completo más el día actual)
        var endDate = DateTime.UtcNow.Date; // Hoy a la medianoche
        var startDate = endDate.AddYears(-1); // Un año atrás

        // Obtener todas las reseñas del usuario
        // Asegúrate de que IReviewService tenga GetReviewsByUserAsync
        var reviews = await _reviewService.GetFriendsReviewsAsync(new List<string> {userId});

        // Obtener todos los seguimientos de juegos del usuario
        // Asegúrate de que GameTrackingDto tenga una propiedad de fecha (ej. CreatedAt o LastUpdatedAt)
        var gameTrackings = await _gameTrackingService.GetTrackingsByUserAsync(userId);

        // Combinar todas las actividades y agruparlas por día
        var allActivities = new List<DateTime>();

        // Añadir fechas de reseñas
        if (reviews != null)
        {
            allActivities.AddRange(reviews.Select(r => r.CreatedAt.Date));
        }

        // Añadir fechas de seguimientos de juegos
        if (gameTrackings != null)
        {
            // ASUMO que GameTrackingDto tiene una propiedad de fecha (ej. LastUpdatedAt o CreatedAt)
            allActivities.AddRange(gameTrackings.Select(gt => gt.LastUpdatedAt.Date)); // Usa la propiedad de fecha correcta
        }
        
        // Agrupar y contar actividades por día
        DailyActivityCounts = allActivities
            .Where(date => date >= startDate && date <= endDate) // Filtrar por el rango del último año
            .GroupBy(date => date.Date) // Agrupar por solo la fecha (sin la hora)
            .ToDictionary(g => g.Key, g => g.Count()); // Convertir a diccionario (Fecha -> Cantidad)

        // Asegurarse de que cada día en el rango tenga al menos 0 actividades
        for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!DailyActivityCounts.ContainsKey(date.Date))
            {
                DailyActivityCounts[date.Date] = 0;
            }
        }
    }
}