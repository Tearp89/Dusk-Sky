using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UI.Pages;

public class DetailsModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public GameModel Game { get; set; } = new();

    public void OnGet()
    {
        // Simulación de datos mientras no hay API real
        var allGames = new List<GameModel>
        {
            new() { Id = 1, Title = "Mysterious Skin", CoverUrl = "/images/mysterious-skin.jpg" },
            new() { Id = 2, Title = "Gran Torino", CoverUrl = "/images/gran-torino.jpg" },
            new() { Id = 3, Title = "Scherzo", CoverUrl = "/images/scherzo.jpg" },
            new() { Id = 4, Title = "All About Lily Chou-Chou", CoverUrl = "/images/lily.jpg" }
        };

        Game = allGames.FirstOrDefault(g => g.Id == Id) ?? new GameModel { Title = "Game Not Found", CoverUrl = "/images/notfound.png" };
    }

    public class GameModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
    }
}
