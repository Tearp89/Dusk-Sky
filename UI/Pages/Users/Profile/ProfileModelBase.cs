// Archivo: Pages/Users/Profile/ProfileModelBase.cs
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq; 
using System; 

public abstract class ProfileModelBase : PageModel
{
    public ProfileHeaderViewModel ProfileHeader { get; set; }
    public bool IsOwnProfile { get; set; }
    public FriendshipStatus Friendship { get; set; } = new FriendshipStatus { Status = "not_friends", RequestId = null }; 
    public string ActiveTab { get; protected set; } 

    // Dependencias para el helper de amistad
    // Estos campos deben ser inicializados por el constructor de la clase concreta (ProfileModel)
    protected IFriendshipService _friendshipService { get; set; } 
    protected IAuthService _authService { get; set; } // Necesario para SearchUserByIdAsync en GetFriendshipStatusForProfileAsync (si lo usas allí)


    // Este método ya no necesitará recibir los servicios como parámetros,
    // sino que los usará como campos de la clase, que serán inyectados por el constructor de ProfileModel.
    protected async Task<bool> LoadProfileHeaderData(
        string userId, // ID del perfil que estamos viendo
        IAuthService authService, // Esto ya no es necesario pasarlo como parámetro si _authService es un campo
        IUserManagerService userManagerService,
        IFriendshipService friendshipService, // Esto ya no es necesario pasarlo como parámetro
        IReviewService reviewService,
        IGameListService listService,
        IGameTrackingService gameTrackingService) 
    {
        // ASIGNAR LOS SERVICIOS A LOS CAMPOS PROTEGIDOS
        _friendshipService = friendshipService;
        _authService = authService;

        string? loggedInUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        IsOwnProfile = (loggedInUserId == userId);

        var authUser = await authService.SearchUserByIdAsync(userId); // Usar authService aquí
        if (authUser == null) return false;

        var profile = await userManagerService.GetProfileAsync(userId);
        
        var reviewsTask = reviewService.GetFriendsReviewsAsync(new List<string> {userId}); 
        var listsTask = listService.GetUserListsAsync(userId);
        var friendsOfProfileUserTask = friendshipService.GetFriendsAsync(userId); 
        await Task.WhenAll(reviewsTask, listsTask, friendsOfProfileUserTask);

        // --- Lógica para determinar el estado de amistad usando el nuevo helper ---
        // Se llama al helper con el ID del usuario logueado y el ID del perfil visitado
        Friendship = await GetFriendshipStatusForProfileAsync(loggedInUserId, userId);

        ProfileHeader = new ProfileHeaderViewModel
        {
            UserId = userId,
            Username = authUser.Username,
            AvatarUrl = profile?.AvatarUrl ?? "/images/default_avatar.png",
            BannerUrl = string.IsNullOrEmpty(profile?.BannerUrl) || profile.BannerUrl.EndsWith(".j_") 
                        ? "/images/default_banner.jpg" 
                        : profile.BannerUrl,
            Bio = profile?.Bio ?? "No bio available.",
            ReviewCount = reviewsTask.Result.Count,
            ListCount = listsTask.Result.Count,
            FriendCount = friendsOfProfileUserTask.Result.Count 
        };

        return true;
    }

    // --- NUEVO MÉTODO HELPER DE AMISTAD (como en SearcherModel) ---
    private async Task<FriendshipStatus> GetFriendshipStatusForProfileAsync(string? loggedInUserId, string profileUserId)
    {
        // Caso 1: Usuario no logueado
        if (string.IsNullOrEmpty(loggedInUserId))
            return new FriendshipStatus { Status = "not_friends", RequestId = null };

        // Caso 2: Es el propio perfil
        if (loggedInUserId == profileUserId)
            return new FriendshipStatus { Status = "self", RequestId = null }; // Searcher usa "is_self", pero "self" es consistente con tu ProfileModelBase

        // --- Obtener datos de amistad y solicitudes para el USUARIO LOGUEADO ---
        var loggedInUserFriends = await _friendshipService.GetFriendsAsync(loggedInUserId); // Amigos del logueado
        var myReceivedPendingRequests = await _friendshipService.GetPendingRequestsAsync(loggedInUserId); // Recibidas por el logueado

        // --- Obtener solicitudes pendientes RECIBIDAS por el PERFIL QUE ESTAMOS VIENDO ---
        // Esto es CLAVE, ya que GetPendingRequestsAsync(ID) devuelve solicitudes donde ID es RECEPTOR.
        // Lo usamos para saber si YO le envié a ÉL.
        var profileUserReceivedPendingRequests = await _friendshipService.GetPendingRequestsAsync(profileUserId); 

        // 1. ¿Son ya amigos? (Ambos usuarios se ven en la lista de amigos del otro)
        if (loggedInUserFriends.Any(f => (f.SenderId == profileUserId && f.ReceiverId == loggedInUserId && f.Status == "accepted") || (f.SenderId == loggedInUserId && f.ReceiverId == profileUserId && f.Status == "accepted")))
        {
            return new FriendshipStatus { Status = "friends", RequestId = null };
        }
        // 2. ¿Hay una solicitud PENDIENTE ENTRANTE (de este perfil para mí)?
        //    (ProfileUserId es el Sender, loggedInUserId es el Receiver)
        else if (myReceivedPendingRequests.Any(r => r.SenderId == profileUserId && r.ReceiverId == loggedInUserId && r.Status == "pending"))
        {
            var incomingRequest = myReceivedPendingRequests.First(r => r.SenderId == profileUserId && r.ReceiverId == loggedInUserId && r.Status == "pending");
            return new FriendshipStatus { Status = "pending_incoming", RequestId = incomingRequest.Id };
        }
        // 3. ¿Hay una solicitud PENDIENTE SALIENTE (mía para este perfil)?
        //    (loggedInUserId es el Sender, ProfileUserId es el Receiver).
        //    Buscamos esto en las solicitudes que el OTRO USUARIO ha RECIBIDO.
        else if (profileUserReceivedPendingRequests.Any(r => r.SenderId == loggedInUserId && r.ReceiverId == profileUserId && r.Status == "pending"))
        {
            var outgoingRequest = profileUserReceivedPendingRequests.First(r => r.SenderId == loggedInUserId && r.ReceiverId == profileUserId && r.Status == "pending");
            return new FriendshipStatus { Status = "pending_outgoing", RequestId = outgoingRequest.Id }; // Podemos devolver el ID de la solicitud saliente también
        }
        // 4. Si ninguna de las anteriores, NO son amigos
        else
        {
            return new FriendshipStatus { Status = "not_friends", RequestId = null };
        }
    }
}