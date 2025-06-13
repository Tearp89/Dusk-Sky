using Microsoft.AspNetCore.Mvc.RazorPages;

public class GamesModel : PageModel
{
    public List<GamePreviewDTO> Games { get; set; }
    private readonly IGameService _gameService;



    public void OnGet()
    {

    }
}
