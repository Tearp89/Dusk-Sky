using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class GamesGeneralModel : PageModel
{
    private readonly IGameService _gameService;

    public Dictionary<string, List<GamePreviewDTO>> CategorizedGames { get; set; }

    public GamesGeneralModel(IGameService gameService)
    {
        _gameService = gameService;
    }

    // En tu archivo Games.cshtml.cs

    public async Task OnGetAsync()
    {
        CategorizedGames = new Dictionary<string, List<GamePreviewDTO>>();

        // --- Categoría: Action ---
        await AddCategoryAsync("Action", new List<string>
    {
        "Counter-Strike 2",
        "Apex Legends",
        "PUBG: BATTLEGROUNDS",
        "Dota 2",
        "Warframe"
    });

        // --- Categoría: Simulation ---
        await AddCategoryAsync("Simulation", new List<string>
    {
        "Euro Truck Simulator 2",
        "Stardew Valley",
        "PowerWash Simulator",
        "Cities: Skylines",
        "Microsoft Flight Simulator 2020"
    });

        // --- Categoría: Strategy ---
        await AddCategoryAsync("Strategy", new List<string>
    {
        "Sid Meier's Civilization VI",
        "Age of Empires II: Definitive Edition",
        "Total War: WARHAMMER III",
        "Stellaris",
        "Crusader Kings III"
    });

        // --- Categoría: Indie ---
        await AddCategoryAsync("Indie", new List<string>
    {
        "Hollow Knight",
        "Celeste",
        "Hades",
        "Terraria",
        "Undertale"
    });

        // ▼▼▼ NUEVA CATEGORÍA AÑADIDA AQUÍ ▼▼▼
        // --- Categoría: Free to Play ---
        await AddCategoryAsync("Free to Play", new List<string>
    {
        "Counter-Strike 2",
        "Dota 2",
        "Apex Legends",
        "Warframe",
        "Destiny 2"
    });
    }

    // Método de ayuda para buscar los juegos de cada categoría y añadirlos al diccionario
    private async Task AddCategoryAsync(string categoryName, List<string> gameTitles)
    {
        var gameDtoList = new List<GamePreviewDTO>();
        foreach (var title in gameTitles)
        {
            // Usamos el método de búsqueda que ya tienes
            var searchResult = await _gameService.SearchGamePreviewsByNameAsync(title);
            var game = searchResult?.FirstOrDefault(); // Tomamos el primer resultado de la búsqueda

            if (game != null)
            {
                gameDtoList.Add(game);
            }
        }

        if (gameDtoList.Any())
        {
            CategorizedGames[categoryName] = gameDtoList;
        }
    }
}