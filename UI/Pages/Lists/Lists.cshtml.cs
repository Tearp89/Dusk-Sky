using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // ✅ Asegúrate de incluir este using

// Asumo que SearchListWithImagesDto, GameListDTO, UserProfileDTO, UserSearchResultDto,
// GamePreviewDTO y GameListItemDTO están en un namespace que ya tienes referenciado.

public class ListsModel : PageModel
{
    private readonly IGameListService _listService;
    private readonly IUserManagerService _userManagerService;
    private readonly IGameListItemService _listItemService;
    private readonly IGameService _gameService;
    private readonly IAuthService _authService;
    private readonly ILogger<ListsModel> _logger; // ✅ Declaración del logger

    public List<SearchListWithImagesDto> RecentLists { get; set; } = new();

    public ListsModel(
        IGameListService listService,
        IUserManagerService userManagerService,
        IGameListItemService listItemService,
        IGameService gameService,
        IAuthService authService,
        ILogger<ListsModel> logger) // ✅ Inyección de ILogger
    {
        // ✅ Validaciones de nulos para los servicios y el logger inyectados en el constructor
        _listService = listService ?? throw new ArgumentNullException(nameof(listService), "GameListService no puede ser nulo.");
        _userManagerService = userManagerService ?? throw new ArgumentNullException(nameof(userManagerService), "UserManagerService no puede ser nulo.");
        _listItemService = listItemService ?? throw new ArgumentNullException(nameof(listItemService), "GameListItemService no puede ser nulo.");
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "GameService no puede ser nulo.");
        _authService = authService ?? throw new ArgumentNullException(nameof(authService), "AuthService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo."); // ✅ Validar el logger
    }

    public async Task OnGetAsync()
    {
        try
        {
            var recentListDTOs = await _listService.GetRecentListsAsync();

            // Validar que recentListDTOs no sea nulo; si es nulo, inicializar como lista vacía
            if (recentListDTOs == null)
            {
                _logger.LogWarning("GetRecentListsAsync devolvió una lista nula. Inicializando como lista vacía."); // ✅ Registro de advertencia
                recentListDTOs = new List<GameListDTO>();
            }

            // Filtrar solo las listas públicas
            var publicListDTOs = recentListDTOs.Where(list => list.IsPublic).ToList();
            _logger.LogInformation("Se encontraron {PublicCount} listas públicas de un total de {TotalCount} listas recientes.", publicListDTOs.Count, recentListDTOs.Count); // ✅ Registro de información

            var tasks = publicListDTOs.Select(async listDto =>
            {
                // Validar que listDto no sea nulo antes de procesar
                if (listDto == null)
                {
                    _logger.LogWarning("Se encontró un GameListDTO nulo durante el procesamiento de listas recientes."); // ✅ Registro de advertencia
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

                // Validar que listDto.UserId no sea nulo o vacío antes de buscar perfiles
                if (!string.IsNullOrEmpty(listDto.UserId))
                {
                    try // ✅ Try-catch para llamadas a servicios individuales dentro del Select
                    {
                        userProfile = await _userManagerService.GetProfileAsync(listDto.UserId);
                        authUser = await _authService.SearchUserByIdAsync(listDto.UserId);
                    }
                    catch (HttpRequestException ex) // ✅ Catch específico para errores de red en estas llamadas
                    {
                        _logger.LogError(ex, "Error de HttpRequestException al obtener perfil/usuario para UserId '{UserId}' en la lista '{ListId}'.", listDto.UserId, listDto.Id);
                    }
                    catch (Exception ex) // ✅ Catch general para otros errores en llamadas a servicios
                    {
                        _logger.LogError(ex, "Error inesperado al obtener perfil/usuario para UserId '{UserId}' en la lista '{ListId}'.", listDto.UserId, listDto.Id);
                    }
                }
                else
                {
                    _logger.LogWarning("ListDto con ID '{ListId}' tiene un UserId nulo o vacío.", listDto.Id); // ✅ Registro de advertencia
                }

                var listItems = await _listItemService.GetItemsByListIdAsync(listDto.Id);

                // Validar que listItems no sea nulo antes de intentar iterar
                var gameImageUrls = new List<string>();
                if (listItems != null)
                {
                    var gameImageTasks = listItems.Take(5).Select(async item =>
                    {
                        // Validar que item no sea nulo y que GameId no sea Guid.Empty
                        if (item == null || item.GameId == Guid.Empty)
                        {
                            _logger.LogWarning("Item de lista inválido (nulo o GameId vacío) encontrado en la lista '{ListId}'.", listDto.Id); // ✅ Registro de advertencia
                            return null;
                        }

                        try // ✅ Try-catch para la llamada a GetGamePreviewByIdAsync
                        {
                            var game = await _gameService.GetGamePreviewByIdAsync(item.GameId);
                            if (game == null)
                            {
                                _logger.LogWarning("GetGamePreviewByIdAsync devolvió nulo para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id); // ✅ Registro de advertencia
                            }
                            return game?.HeaderUrl;
                        }
                        catch (HttpRequestException ex) // ✅ Catch específico para errores de red
                        {
                            _logger.LogError(ex, "Error de HttpRequestException al obtener GamePreview para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            return null;
                        }
                        catch (Exception ex) // ✅ Catch general
                        {
                            _logger.LogError(ex, "Error inesperado al obtener GamePreview para GameId '{GameId}' en la lista '{ListId}'.", item.GameId, listDto.Id);
                            return null;
                        }
                    });
                    gameImageUrls = (await Task.WhenAll(gameImageTasks)).Where(url => !string.IsNullOrEmpty(url)).ToList();
                }
                else
                {
                    _logger.LogInformation("No se encontraron items para la lista '{ListId}'.", listDto.Id); // ✅ Registro de información
                }

                // Creamos una instancia de tu SearchListWithImagesDto
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
            _logger.LogInformation("Carga de listas recientes finalizada exitosamente."); // ✅ Registro de información
        }
        catch (ArgumentNullException ex) // ✅ Catch específico para ArgumentNullException
        {
            _logger.LogError(ex, "ArgumentNullException en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Hubo un problema de datos al cargar las listas. Por favor, inténtalo de nuevo más tarde.";
            RecentLists = new List<SearchListWithImagesDto>(); // ✅ Asegurar que la lista esté vacía en caso de error
        }
        catch (InvalidOperationException ex) // ✅ Catch específico para InvalidOperationException
        {
            _logger.LogError(ex, "InvalidOperationException en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "No se pudieron procesar algunas operaciones al cargar las listas. Inténtalo más tarde.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
        catch (HttpRequestException ex) // ✅ Catch específico para problemas de red generales
        {
            _logger.LogError(ex, "HttpRequestException en OnGetAsync al comunicarse con servicios externos: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Problema de conexión al cargar las listas. Por favor, verifica tu internet.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
        catch (Exception ex) // ✅ Catch general para cualquier otra excepción inesperada
        {
            _logger.LogError(ex, "Error inesperado en OnGetAsync al cargar listas: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar las listas. Por favor, inténtalo de nuevo.";
            RecentLists = new List<SearchListWithImagesDto>();
        }
    }
}