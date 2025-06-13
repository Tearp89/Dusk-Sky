using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class ListCreatorModel : PageModel
{
    [BindProperty]
    public string? DefaultName { get; set; }

    [BindProperty]
    public string? GameId { get; set; }
    [BindProperty] public string ListName { get; set; } = string.Empty;
    [BindProperty] public string Tags { get; set; } = string.Empty;
    [BindProperty] public string Visibility { get; set; } = "private";
    [BindProperty] public bool Ranked { get; set; } = false;
    [BindProperty] public string Description { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? defaultName, string? gameId)
    {
        DefaultName = defaultName;
        GameId = gameId;

        // Aquí podrías cargar info del juego si GameId != null
        // Ejemplo:
        // if (!string.IsNullOrEmpty(GameId))
        //     GamePreview = await _gameService.GetGameByIdAsync(GameId);

        return Page();
    }
}
