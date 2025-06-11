using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


public class GameSearchModel : PageModel
{
    private readonly IGameService _gameService;

    public GameSearchModel(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task<IActionResult> OnGetSearchAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return new JsonResult(new List<GamePreviewDTO>());

        try
        {
            var results = await _gameService.SearchGamePreviewsByNameAsync(term);
            return new JsonResult(results);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al buscar juegos: " + ex.Message);
            return StatusCode(500, "Error interno");
        }
    }
}
