using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using



public class ListsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IUserManagerService _userManagerService;
    private readonly IGameListItemService _listItemService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly ILogger<ListsModel> _logger; 

    public List<SearchListWithImagesDto> RecentLists { get; set; } = new();

    public ListsModel(
        IGameListService listService,
        IUserManagerService userManagerService,
        IGameListItemService listItemService,
        IGameService gameService,
        IAuthService authService,
        ILogger<ListsModel> logger) 
    {
        _listService = listService ?? throw new ArgumentNullException(nameof(listService), "GameListService no puede ser nulo.");
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "UserManagerService no puede ser nulo.");
        _listItemService = listItemService ?? throw new ArgumentNullException(nameof(listItemService), "GameListItemService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "GameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "AuthService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); 
    }

    public async Task OnGetAsync()
    {
        try
        {
            var recentListDTOs = await _listService.GetRecentListsAsync();

            if (recentListDTOs == null)
            {
                _logger.LogWarning("GetRecentListsAsync devolvió una lista nula. Inicializando como lista vacía."); 
                recentListDTOs = new List<GameListDTO>();
            }

            var publicListDTOs = recentListDTOs.Where(list => list.IsPublic).ToList();
            _logger.LogInformation("Se encontraron {PublicCount} listas públicas de un total de {TotalCount} listas recientes.", publicListDTOs.Count, recentListDTOs.Count); 

            var tasks = publicListDTOs.Select(async listDto =>
            {
                if (listDto == null)
                {
                    _logger.LogWarning("Se encontró un GameListDTO nulo durante el procesamiento de listas recientes."); 
                    return new SearchListWithImagesDto
                    {
                        Id = Guid.Empty.ToString(),
                        Name = "Lista no disponible",
                        Description = "Descripción no disponible",
                        UserName = "Usuario Desconocido",
                        AvatarUrl = "/images/default-avatar.png",
                        GameHeaders = new List<string>()
                    };
                }

                UserProfileDTO? userProfile = null;
                UserSearchResultDto? authUser = null;

                if (!string.IsNullOrEmpty(listDto.UserId))
                {
                    try 
                    {
                        userProfile = await _userManagerService.GetProfileAsync(listDto.UserId);
                        authUser = await _authService.SearchUserByIdAsync(listDto.UserId);
                    }
                    catch (HttpRequestException ex) 
                    {
                        _logger.LogError(ex, "Error de HttpRequestException al obtener perfil/usuario para UserId '{UserId}' en la lista '{ListId}'.", listDto.UserId, listDto.Id);
                    }
                    catch (Exception ex) 
                    {
                        _logger.LogError(ex, "Error inesperado al obtener perfil/usuario para UserId '{UserId}' en la lista '{ListId}'.", listDto.UserId, listDto.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("ListDto con ID '{ListId}' tiene un UserId nulo o vacío.", listDto.Id); 
                }

                var listItems = await _listItemService.GetItemsByListIdAsync(listDto.Id);

                var gameImageUrls = new List<string>();
                if (listItems != null)
                {
                    var gameImageTasks = listItems.Take(5).Select(async item =>
                    {
                        if (item == null || item.GameId == Guid.Empty)
                        {
                            _logger.LogWarning("Item de lista inválido (nulo o GameId vacío) encontrado en la lista '{ListId}'.", listDto.Id); 
                            return null;
                        }

                        try 
                        {
                            var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                            if (game == null)
                            {
                                _logger.LogWarning("GetGamePreviewByIdAsync devolvió nulo para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id); 
                            }
                            return game?.HeaderUrl;
                        }
                        catch (HttpRequestException ex) 
                        {
                            _logger.LogError(ex, "Error de HttpRequestException al obtener GamePreview para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            return null;
                        }
                        catch (Exception ex) 
                        {
                            _logger.LogError(ex, "Error inesperado al obtener GamePreview para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            return null;
                        }
                    });
                    gameImageUrls = (await Task.WhenAll(gameImageTasks)).Where(url => !string.IsNullOrEmpty(url)).ToList();
                }
                else
                {
                    _logger.LogInformation("No se encontraron items para la lista '{ListId}'.", listDto.Id); 
                }

                return new SearchListWithImagesDto
                {
                    Id = listDto.Id,
                    Name = listDto.Name,
                    Description = listDto.Description,
                    UserId = listDto.UserId,
                    UserName = !string.IsNullOrWhiteSpace(authUser?.Username) ? authUser.Username : "Usuario Desconocido",
                    AvatarUrl = userProfile?.AvatarUrl ?? "/images/default-avatar.png",
                    GameHeaders = gameImageUrls
                };
            });

            RecentLists = (await Task.WhenAll(tasks)).ToList();
            _logger.LogInformation("Carga de listas recientes finalizada exitosamente."); 
        }
        catch (ArgumentNullException ex) 
        {
            _logger.LogError(ex, "ArgumentNullException en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Hubo un problema de datos al cargar las listas. Por favor, inténtalo de nuevo más tarde.";
            RecentLists = new List<SearchListWithImagesDto>(); 
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "InvalidOperationException en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "No se pudieron procesar algunas operaciones al cargar las listas. Inténtalo más tarde.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HttpRequestException en OnGetAsync al comunicarse con servicios externos: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Problema de conexión al cargar las listas. Por favor, verifica tu internet.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Error inesperado en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar las listas. Por favor, inténtalo de nuevo.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
    }
}