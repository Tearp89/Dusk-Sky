using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GamesGeneralModel : PageModel
{
    private readonly IGameService _gameService;
    private readonly ILogger<GamesGeneralModel> _logger; 

    public Dictionary<string, List<GamePreviewDTO>> CategorizedGames { get; set; } = new();

    public GamesGeneralModel(IGameService gameService, ILogger<GamesGeneralModel> logger)
    {
        _gameService = gameService ?? throw new ArgumentNullException(nameof(gameService), "IGameService no puede ser nulo.");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "ILogger no puede ser nulo.");
    }

    public async Task OnGetAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando la carga de juegos por categoría.");

            var categoryTasks = new List<Task>
            {
                AddCategoryAsync("Action", new List<string> { "Counter-Strike 2", "Apex Legends", "PUBG: BATTLEGROUNDS", "Dota 2", "Warframe" }),
                AddCategoryAsync("Simulation", new List<string> { "Euro Truck Simulator 2", "Stardew Valley", "PowerWash Simulator", "Cities: Skylines", "Microsoft Flight Simulator 2020" }),
                AddCategoryAsync("Strategy", new List<string> { "Sid Meier's Civilization VI", "Age of Empires II: Definitive Edition", "Total War: WARHAMMER III", "Stellaris", "Crusader Kings III" }),
                AddCategoryAsync("Indie", new List<string> { "Hollow Knight", "Celeste", "Hades", "Terraria", "Undertale" }),
                AddCategoryAsync("Free to Play", new List<string> { "Counter-Strike 2", "Dota 2", "Apex Legends", "Warframe", "Destiny 2" })
            };

            await Task.WhenAll(categoryTasks);

            _logger.LogInformation("Carga de juegos por categoría completada exitosamente. Total de categorías procesadas: {CategoryCount}", CategorizedGames.Count); // ✅ Registro de información
        }
        catch (ArgumentException ex) 
        {
            _logger.LogError(ex, "ArgumentException en OnGetAsync al cargar juegos generales: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Hubo un problema con los datos de entrada al cargar los juegos. Por favor, inténtalo de nuevo más tarde.";
            CategorizedGames = new Dictionary<string, List<GamePreviewDTO>>(); 
        }
        catch (InvalidOperationException ex) 
        {
            _logger.LogError(ex, "InvalidOperationException en OnGetAsync al cargar juegos generales: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "No se pudieron procesar algunas operaciones al cargar los juegos. Inténtalo más tarde.";
            CategorizedGames = new Dictionary<string, List<GamePreviewDTO>>();
        }
        catch (HttpRequestException ex) 
        {
            _logger.LogError(ex, "HttpRequestException en OnGetAsync al comunicarse con el servicio de juegos: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Problema de conexión al cargar los juegos. Por favor, verifica tu internet.";
            CategorizedGames = new Dictionary<string, List<GamePreviewDTO>>();
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex, "Ocurrió un error inesperado en OnGetAsync al cargar la página de juegos generales: {ErrorMessage}", ex.Message);
            TempData["ErrorMessage"] = "Ocurrió un error inesperado al cargar los juegos. Por favor, inténtalo de nuevo más tarde.";
            CategorizedGames = new Dictionary<string, List<GamePreviewDTO>>();
        }
    }

    private async Task AddCategoryAsync(string categoryName, List<string> gameTitles)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            _logger.LogWarning("AddCategoryAsync: categoryName es nulo o vacío."); 
            return;
        }
        if (gameTitles == null || !gameTitles.Any())
        {
            _logger.LogWarning("AddCategoryAsync: La lista de gameTitles es nula o vacía para la categoría '{CategoryName}'.", categoryName); // ✅ Registro de advertencia
            return;
        }

        var gameDtoList = new List<GamePreviewDTO>();

        var searchTasks = gameTitles.Select(async title =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    _logger.LogWarning("AddCategoryAsync: Título de juego nulo o vacío encontrado para la categoría '{CategoryName}'. Saltando.", categoryName); // ✅ Registro de advertencia
                    return null;
                }

                var searchResult = await _gameService.SearchGamePreviewsByNameAsync(title);
                if (searchResult == null || !searchResult.Any())
                {
                    _logger.LogInformation("AddCategoryAsync: No se encontraron resultados de vista previa para el juego '{GameTitle}' en la categoría '{CategoryName}'.", title, categoryName); // ✅ Registro de información
                    return null;
                }
                return searchResult.FirstOrDefault();
            }
            catch (HttpRequestException ex) 
            {
                _logger.LogError(ex, "HttpRequestException al cargar el juego '{GameTitle}' para la categoría '{CategoryName}'. Mensaje: {Message}", title, categoryName, ex.Message); // ✅ Registro de error
                return null; 
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error inesperado al cargar el juego '{GameTitle}' para la categoría '{CategoryName}'. Mensaje: {Message}", title, categoryName, ex.Message); // ✅ Registro de error
                return null;
            }
        }).ToList();

        var games = await Task.WhenAll(searchTasks);

        gameDtoList.AddRange(games.Where(g => g != null).Select(g => g!)); 

        
        if (gameDtoList.Any())
        {
            CategorizedGames.TryAdd(categoryName, gameDtoList);
            _logger.LogDebug("AddCategoryAsync: Categoría '{CategoryName}' añadida con {GameCount} juegos.", categoryName, gameDtoList.Count); 
        }
        else
        {
            _logger.LogInformation("AddCategoryAsync: No se añadieron juegos a la categoría '{CategoryName}'.", categoryName); 
        }
    }
}