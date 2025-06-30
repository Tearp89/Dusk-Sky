using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging; // ¡Importante para el logging!

public class SearcherModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly IReviewService _reviewService;
    private readonly IGameListService _listService;
    private readonly IUserManagerService _userManagerService;
    private readonly IGameListItemService _listItemService;
    private readonly IFriendshipService _friendshipService;
    private readonly ILogger<SearcherModel> _logger; // ¡Nuevo: Inyección del logger!

    public SearcherModel(
        IGameService gameService,
        IAuthService authService,
        IReviewService reviewService,
        IGameListService listService,
        IUserManagerService userManagerService,
        IGameListItemService listItemService,
        IFriendshipService friendshipService,
        ILogger<SearcherModel> logger) // ¡Nuevo: Parámetro del logger en el constructor!
    {
        _gameService = gameService;
        _authService = authService;
        _reviewService = reviewService;
        _listService = listService;
        _userManagerService = userManagerService;
        _listItemService = listItemService;
        _friendshipService = friendshipService;
        _logger = logger; // Asignación del logger
    }

    [BindProperty(SupportsGet = true)]
    public string Query { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Filter { get; set; }

    public List<GamePreviewDTO> Games { get; set; } = new();
    public List<SearchUserWithFriendshipStatusDto> Users { get; set; } = new();
    public List<ReviewFullDto> Reviews { get; set; } = new();
    public List<SearchListWithImagesDto> Lists { get; set; } = new();

    // No es necesario que sean privados en esta clase si se usan solo en OnGetAsync
    // pero si se usan en otros lugares, es buena idea mantenerlos como propiedades
    // o pasarlos como parámetros. Para este ejemplo, los mantengo como los tenías.
    private HashSet<string>? FriendIds; // Hago que sea nullable para la comprobación de null
    private Dictionary<string, string>? IncomingRequestSenders; // Hago que sea nullable

    public async Task OnGetAsync()
    {
        _logger.LogInformation("Iniciando OnGetAsync con Query: '{Query}', Filter: '{Filter}'", Query, Filter);

        if (string.IsNullOrWhiteSpace(Query))
        {
            _logger.LogInformation("Query es nulo o vacío, retornando sin búsqueda.");
            return;
        }

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            try
            {
                _logger.LogInformation("Obteniendo amigos para el usuario: {UserId}", currentUserId);
                var friends = await _friendshipService.GetFriendsAsync(currentUserId);
                // Validación de null: Asegurarse de que 'friends' no sea null
                if (friends == null)
                {
                    _logger.LogWarning("GetFriendsAsync devolvió null para el usuario {UserId}.", currentUserId);
                    FriendIds = new HashSet<string>(); // Inicializar para evitar NRE
                }
                else
                {
                    FriendIds = friends
                        .Select(f => f.SenderId == currentUserId ? f.ReceiverId : f.SenderId)
                        .ToHashSet();
                    _logger.LogInformation("Se encontraron {Count} amigos para el usuario {UserId}.", FriendIds.Count, currentUserId);
                }

                _logger.LogInformation("Obteniendo solicitudes pendientes para el usuario: {UserId}", currentUserId);
                var incomingRequests = await _friendshipService.GetPendingRequestsAsync(currentUserId);
                // Validación de null: Asegurarse de que 'incomingRequests' no sea null
                if (incomingRequests == null)
                {
                    _logger.LogWarning("GetPendingRequestsAsync devolvió null para el usuario {UserId}.", currentUserId);
                    IncomingRequestSenders = new Dictionary<string, string>(); // Inicializar
                }
                else
                {
                    IncomingRequestSenders = incomingRequests.ToDictionary(r => r.SenderId, r => r.Id);
                    _logger.LogInformation("Se encontraron {Count} solicitudes entrantes para el usuario {UserId}.", IncomingRequestSenders.Count, currentUserId);
                }
            }
            catch (HttpRequestException httpEx) // Ejemplo de excepción específica para servicios web
            {
                _logger.LogError(httpEx, "Error de red al obtener amigos o solicitudes para el usuario {UserId}.", currentUserId);
                // Podrías manejar esto de forma más amigable al usuario
            }
            catch (Exception ex) // Excepción general
            {
                _logger.LogError(ex, "Ocurrió un error inesperado al inicializar datos de amistad para el usuario {UserId}.", currentUserId);
            }
        }
        else
        {
            _logger.LogWarning("CurrentUserId es nulo o vacío, no se cargarán datos de amistad.");
        }


        // Bloques try-catch para las llamadas principales de búsqueda
        try
        {
            if (Filter is null or "all")
            {
                _logger.LogInformation("Filtro 'all' detectado. Realizando búsqueda de juegos, usuarios, reseñas y listas.");
                Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
                await LoadUsersAsync(Query);
                await LoadReviewsAsync(Query);
                await LoadListsAsync(Query);
            }
            else if (Filter == "games")
            {
                _logger.LogInformation("Filtro 'games' detectado. Buscando juegos.");
                Games = await _gameService.SearchGamePreviewsByNameAsync(Query);
            }
            else if (Filter == "users")
            {
                _logger.LogInformation("Filtro 'users' detectado. Buscando usuarios.");
                await LoadUsersAsync(Query);
            }
            else if (Filter == "reviews")
            {
                _logger.LogInformation("Filtro 'reviews' detectado. Buscando reseñas.");
                await LoadReviewsAsync(Query);
            }
            else if (Filter == "lists")
            {
                _logger.LogInformation("Filtro 'lists' detectado. Buscando listas.");
                await LoadListsAsync(Query);
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Error de red al realizar la búsqueda con el filtro '{Filter}' y la consulta '{Query}'.", Filter, Query);
            // Considera establecer un mensaje de error amigable al usuario en TempData
            TempData["ErrorMessage"] = "Hubo un problema de conexión al buscar. Por favor, inténtalo de nuevo más tarde.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ocurrió un error inesperado al realizar la búsqueda con el filtro '{Filter}' y la consulta '{Query}'.", Filter, Query);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado durante la búsqueda. Por favor, inténtalo de nuevo.";
        }

        _logger.LogInformation("OnGetAsync finalizado.");
    }

    private async Task LoadUsersAsync(string query)
    {
        _logger.LogInformation("Cargando usuarios con consulta: {Query}", query);
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        try
        {
            var userMatches = await _authService.SearchUsersAsync(query);
            // Validación de null: userMatches podría ser null si el servicio falla o no encuentra nada.
            if (userMatches == null)
            {
                _logger.LogWarning("SearchUsersAsync devolvió null para la consulta '{Query}'.", query);
                Users = new List<SearchUserWithFriendshipStatusDto>();
                return;
            }

            var userTasks = userMatches.Select(async user =>
            {
                string avatarUrl = "/Images/noImage.png";
                try
                {
                    // Validación de null: user.Id
                    if (string.IsNullOrEmpty(user.Id))
                    {
                        _logger.LogWarning("Usuario con ID nulo o vacío encontrado al buscar perfiles.");
                        return null; // Retorna null para filtrar más tarde
                    }

                    var profile = await _userManagerService.GetProfileAsync(user.Id);
                    // Validación de null: profile
                    if (profile == null)
                    {
                        _logger.LogWarning("Perfil no encontrado para el usuario ID: {UserId}. Usando avatar predeterminado.", user.Id);
                        avatarUrl = "/Images/noImage.png"; // Asegura que se usa el valor por defecto
                    }
                    else
                    {
                        avatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
                    }
                }
                catch (Exception innerEx) // Captura excepciones específicas al obtener el perfil
                {
                    _logger.LogError(innerEx, "Error al obtener perfil o avatar para el usuario ID: {UserId}", user.Id);
                    avatarUrl = "/Images/noImage.png"; // Fallback en caso de error
                }

                // Asegúrate de que currentUserId y user.Id no sean null para GetFriendshipStatus
                // La función GetFriendshipStatus ya maneja currentUserId nulo.
                // Si user.Id fuera null, el Select(async user => ...) ya lo filtraría.
                var friendshipStatus = await GetFriendshipStatus(currentUserId!, user.Id); // currentUserId ya fue validado en OnGetAsync

                return new SearchUserWithFriendshipStatusDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    AvatarUrl = avatarUrl,
                    Friendship = friendshipStatus
                };
            }).Where(dto => dto != null); // Filtrar resultados nulos si se retornó null en el Select

            Users = (await Task.WhenAll(userTasks)).ToList()!; // ! para indicar que se espera que no haya nulls después del filtro
            _logger.LogInformation("Carga de usuarios completada. Se encontraron {Count} usuarios.", Users.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en LoadUsersAsync con consulta: '{Query}'.", query);
            Users = new List<SearchUserWithFriendshipStatusDto>(); // Asegurar que la lista no sea null
        }
    }

    private async Task<FriendshipStatus> GetFriendshipStatus(string? currentUserId, string otherUserId)
    {
        _logger.LogDebug("Obteniendo estado de amistad para currentUserId: '{CurrentUserId}', otherUserId: '{OtherUserId}'", currentUserId, otherUserId);

        // Validación de null: otherUserId es crítico aquí. Si es null, no podemos continuar.
        if (string.IsNullOrEmpty(otherUserId))
        {
            _logger.LogError("otherUserId es nulo o vacío en GetFriendshipStatus.");
            throw new ArgumentNullException(nameof(otherUserId), "otherUserId no puede ser nulo o vacío.");
        }

        if (string.IsNullOrEmpty(currentUserId))
        {
            _logger.LogDebug("CurrentUserId es nulo, retornando 'not_friends'.");
            return new FriendshipStatus { Status = "not_friends" };
        }

        if (currentUserId == otherUserId)
        {
            _logger.LogDebug("CurrentUserId es igual a otherUserId, retornando 'is_self'.");
            return new FriendshipStatus { Status = "is_self" };
        }

        // Acceso seguro a FriendIds y IncomingRequestSenders (son nullables)
        if (FriendIds?.Contains(otherUserId) == true)
        {
            _logger.LogDebug("Usuario {OtherUserId} es amigo de {CurrentUserId}.", otherUserId, currentUserId);
            return new FriendshipStatus { Status = "friends" };
        }

        if (IncomingRequestSenders?.TryGetValue(otherUserId, out var incomingRequestId) == true)
        {
            _logger.LogDebug("Hay una solicitud entrante de {OtherUserId} a {CurrentUserId}.", otherUserId, currentUserId);
            return new FriendshipStatus { Status = "pending_incoming", RequestId = incomingRequestId };
        }

        try
        {
            _logger.LogDebug("Buscando solicitudes pendientes salientes de {CurrentUserId} a {OtherUserId}.", currentUserId, otherUserId);
            var otherUserPendingRequests = await _friendshipService.GetPendingRequestsAsync(otherUserId);
            // Validación de null: otherUserPendingRequests
            if (otherUserPendingRequests == null)
            {
                _logger.LogWarning("GetPendingRequestsAsync devolvió null para el usuario {OtherUserId}.", otherUserId);
                return new FriendshipStatus { Status = "not_friends" };
            }

            var sentRequest = otherUserPendingRequests.FirstOrDefault(r => r.SenderId == currentUserId);
            if (sentRequest != null)
            {
                _logger.LogDebug("Existe una solicitud saliente de {CurrentUserId} a {OtherUserId}.", currentUserId, otherUserId);
                return new FriendshipStatus { Status = "pending_outgoing", RequestId = sentRequest.Id };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener solicitudes pendientes de {OtherUserId} en GetFriendshipStatus.", otherUserId);
            // Fallback en caso de error
        }

        _logger.LogDebug("No se encontró estado de amistad específico, retornando 'not_friends'.");
        return new FriendshipStatus { Status = "not_friends" };
    }

    public async Task<IActionResult> OnPostAcceptFriendRequestAsync(string requestId)
    {
        _logger.LogInformation("Intento de aceptar solicitud de amistad con RequestId: {RequestId}", requestId);

        // Validación de null: requestId
        if (string.IsNullOrEmpty(requestId))
        {
            _logger.LogWarning("RequestId es nulo o vacío al intentar aceptar solicitud.");
            TempData["ErrorMessage"] = "No se pudo aceptar la solicitud. ID de solicitud no válido.";
            return RedirectToPage(new { query = Query, filter = Filter });
        }

        try
        {
            var result = await _friendshipService.AcceptRequestAsync(requestId);

            if (result)
            {
                _logger.LogInformation("Solicitud de amistad {RequestId} aceptada exitosamente.", requestId);
                TempData["SuccessMessage"] = "Amigo añadido exitosamente.";
            }
            else
            {
                _logger.LogWarning("El servicio no pudo aceptar la solicitud de amistad {RequestId}.", requestId);
                TempData["ErrorMessage"] = "No se pudo aceptar la solicitud.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar aceptar la solicitud de amistad {RequestId}.", requestId);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al aceptar la solicitud.";
        }

        return RedirectToPage(new { query = Query, filter = Filter });
    }

    private async Task LoadReviewsAsync(string query)
    {
        _logger.LogInformation("Cargando reseñas con consulta: {Query}", query);
        try
        {
            var recentReviews = await _reviewService.GetRecentReviewsAsync();
            // Validación de null: recentReviews
            if (recentReviews == null)
            {
                _logger.LogWarning("GetRecentReviewsAsync devolvió null.");
                Reviews = new List<ReviewFullDto>();
                return;
            }

            var matchedGames = await _gameService.SearchGamePreviewsByNameAsync(query);
            // Validación de null: matchedGames
            var matchedGameIds = matchedGames?.Select(g => g.Id.ToString()).ToHashSet() ?? new HashSet<string>();

            var filtered = recentReviews
                .Where(r =>
                    r.Content != null && r.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    matchedGameIds.Contains(r.GameId.ToString())
                ).ToList();

            var reviewTasks = filtered.Select(async r =>
            {
                string userName = "Unknown";
                string avatarUrl = "/Images/noImage.png";
                string gameTitle = "Unknown Game";
                string gameImage = "/Images/noImage.png";

                // Validación de null: r.UserId
                if (string.IsNullOrEmpty(r.UserId))
                {
                    _logger.LogWarning("Reseña con UserId nulo o vacío encontrada. No se puede obtener el perfil/usuario.");
                    // Podríamos saltar este DTO o rellenar con valores predeterminados
                }
                else
                {
                    try
                    {
                        var profile = await _userManagerService.GetProfileAsync(r.UserId);
                        var username = await _authService.SearchUserByIdAsync(r.UserId);

                        // Validación de null: profile y username
                        if (profile != null)
                        {
                            userName = username?.Username ?? "Unknown";
                            avatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
                        }
                        else
                        {
                            _logger.LogWarning("Perfil no encontrado para el usuario {UserId} de la reseña {ReviewId}.", r.UserId, r.Id);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error al obtener perfil o username para la reseña {ReviewId} de UserId: {UserId}", r.Id, r.UserId);
                    }
                }


                // Validación de null: r.GameId
                if (string.IsNullOrEmpty(r.GameId.ToString()))
                {
                    _logger.LogWarning("Reseña con GameId nulo o vacío encontrada. No se puede obtener el juego.");
                }
                else
                {
                    try
                    {
                        var game = await _gameService.GetGamePreviewByIdAsync(r.GameId);
                        // Validación de null: game
                        if (game != null)
                        {
                            gameTitle = game.Title;
                            gameImage = game.HeaderUrl ?? "/Images/noImage.png";
                        }
                        else
                        {
                            _logger.LogWarning("Juego no encontrado para GameId: {GameId} de la reseña {ReviewId}.", r.GameId, r.Id);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error al obtener juego para GameId: {GameId} de la reseña {ReviewId}", r.GameId, r.Id);
                    }
                }

                return new ReviewFullDto
                {
                    Id = r.Id,
                    GameId = r.GameId.ToString() ?? string.Empty, // Asegurar que no sea null
                    GameTitle = gameTitle,
                    GameImageUrl = gameImage,
                    UserId = r.UserId,
                    UserName = userName,
                    ProfileImageUrl = avatarUrl,
                    Content = r.Content ?? string.Empty, // Asegurar que no sea null
                    Rating = r.Rating,
                    CreatedAt = r.CreatedAt,
                    Likes = r.Likes,
                    LikedBy = r.LikedBy,
                    UserLiked = false
                };
            });

            Reviews = (await Task.WhenAll(reviewTasks)).ToList();
            _logger.LogInformation("Carga de reseñas completada. Se encontraron {Count} reseñas.", Reviews.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en LoadReviewsAsync con consulta: '{Query}'.", query);
            Reviews = new List<ReviewFullDto>();
        }
    }

    private async Task LoadListsAsync(string query)
    {
        _logger.LogInformation("Cargando listas con consulta: {Query}", query);
        try
        {
            var allRecentLists = await _listService.GetRecentListsAsync();
            // Validación de null: allRecentLists
            if (allRecentLists == null)
            {
                _logger.LogWarning("GetRecentListsAsync devolvió null.");
                Lists = new List<SearchListWithImagesDto>();
                return;
            }

            var publicLists = allRecentLists.Where(list => list.IsPublic); // Asumiendo que IsPublic existe

            var matchedGames = await _gameService.SearchGamePreviewsByNameAsync(query);
            // Validación de null: matchedGames
            var matchedGameIds = matchedGames?.Select(g => g.Id.ToString()).ToHashSet() ?? new HashSet<string>();

            var listTasks = publicLists.Select(async list =>
            {
                var dto = new SearchListWithImagesDto
                {
                    Id = list.Id,
                    Name = list.Name,
                    Description = list.Description,
                    UserId = list.UserId,
                    IsPublic = list.IsPublic,
                    GameHeaders = new()
                };

                // Validación de null: list.UserId
                if (string.IsNullOrEmpty(list.UserId))
                {
                    _logger.LogWarning("Lista con UserId nulo o vacío encontrada. No se puede obtener el perfil/usuario para la lista {ListId}.", list.Id);
                }
                else
                {
                    try
                    {
                        var profile = await _userManagerService.GetProfileAsync(list.UserId);
                        var username = await _authService.SearchUserByIdAsync(list.UserId);
                        if (profile != null)
                        {
                            dto.UserName = username?.Username ?? "Unknown";
                            dto.AvatarUrl = profile.AvatarUrl ?? "/Images/noImage.png";
                        }
                        else
                        {
                            _logger.LogWarning("Perfil no encontrado para el usuario {UserId} de la lista {ListId}.", list.UserId, list.Id);
                        }
                    }
                    catch (Exception innerEx)
                    {
                        _logger.LogError(innerEx, "Error al obtener perfil o username para la lista {ListId} de UserId: {UserId}", list.Id, list.UserId);
                    }
                }

                List<GameListItemDTO>? gameItems = null; // Inicializar a null
                try
                {
                    gameItems = await _listItemService.GetItemsByListIdAsync(list.Id);
                    // Validación de null: gameItems
                    if (gameItems == null)
                    {
                        _logger.LogWarning("GetItemsByListIdAsync devolvió null para la lista {ListId}.", list.Id);
                        gameItems = new List<GameListItemDTO>(); // Asegurar que sea una lista vacía para el resto de la lógica
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, "Error al obtener elementos de la lista {ListId}.", list.Id);
                    gameItems = new List<GameListItemDTO>(); // En caso de error, inicializar como vacío
                }


                var matchedInList = gameItems
                    .Where(i => matchedGameIds.Contains(i.GameId.ToString()))
                    .Select(i => matchedGames.FirstOrDefault(g => g.Id == i.GameId)?.HeaderUrl ?? "")
                    .Where(url => !string.IsNullOrEmpty(url))
                    .Take(6)
                    .ToList();

                if (matchedInList.Any())
                {
                    dto.GameHeaders = matchedInList;
                }
                else
                {
                    var headerTasks = gameItems
                        .Select(i => i.GameId)
                        .Distinct()
                        .Take(6)
                        .Select(async id =>
                        {
                            try
                            {
                                var game = await _gameService.GetGamePreviewByIdAsync(id);
                                return game?.HeaderUrl ?? "";
                            }
                            catch (Exception innerGameEx)
                            {
                                _logger.LogError(innerGameEx, "Error al obtener vista previa del juego por ID {GameId} para la lista {ListId}.", id, list.Id);
                                return ""; // Fallback
                            }
                        });

                    var headers = await Task.WhenAll(headerTasks);
                    dto.GameHeaders = headers.Where(url => !string.IsNullOrEmpty(url)).ToList();
                }

                // Asegurar que Name y Description no sean null antes de usar Contains
                bool nameMatch = (!string.IsNullOrEmpty(dto.Name) && dto.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
                bool descMatch = (!string.IsNullOrEmpty(dto.Description) && dto.Description.Contains(query, StringComparison.OrdinalIgnoreCase));
                bool gameMatch = gameItems.Any(i => matchedGameIds.Contains(i.GameId.ToString()));

                return (matches: nameMatch || descMatch || gameMatch, dto);
            });

            Lists = (await Task.WhenAll(listTasks))
                .Where(t => t.matches)
                .Select(t => t.dto)
                .ToList();
            _logger.LogInformation("Carga de listas completada. Se encontraron {Count} listas.", Lists.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado en LoadListsAsync con consulta: '{Query}'.", query);
            Lists = new List<SearchListWithImagesDto>();
        }
    }

    public async Task<IActionResult> OnPostSendFriendRequestAsync(string receiverId)
    {
        _logger.LogInformation("Intento de enviar solicitud de amistad a ReceiverId: {ReceiverId}", receiverId);
        var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Validación de null: senderId y receiverId
        if (string.IsNullOrEmpty(senderId))
        {
            _logger.LogWarning("SenderId es nulo o vacío al intentar enviar solicitud.");
            TempData["ErrorMessage"] = "No se pudo enviar la solicitud. Tu ID de usuario no es válido.";
            return RedirectToPage(new { query = Query, filter = Filter }); // fallback
        }
        if (string.IsNullOrEmpty(receiverId))
        {
            _logger.LogWarning("ReceiverId es nulo o vacío al intentar enviar solicitud.");
            TempData["ErrorMessage"] = "No se pudo enviar la solicitud. ID del destinatario no válido.";
            return RedirectToPage(new { query = Query, filter = Filter }); // fallback
        }

        try
        {
            var result = await _friendshipService.SendRequestAsync(senderId, receiverId);
            if (result)
            {
                _logger.LogInformation("Solicitud de amistad enviada exitosamente de {SenderId} a {ReceiverId}.", senderId, receiverId);
                TempData["SuccessMessage"] = "Solicitud enviada.";
            }
            else
            {
                _logger.LogWarning("El servicio no pudo enviar la solicitud de amistad de {SenderId} a {ReceiverId}.", senderId, receiverId);
                TempData["ErrorMessage"] = "Hubo un problema al enviar la solicitud, inténtalo de nuevo más tarde.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar enviar la solicitud de amistad de {SenderId} a {ReceiverId}.", senderId, receiverId);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al enviar la solicitud.";
        }
        return RedirectToPage(new { query = Query, filter = Filter });
    }
}

