using Microsoft.AspNetCore.Mvc.RazorPages;

public class SearcherModel : PageModel
{
    public List<GamePreviewDTO> Games { get; set; }
    private readonly IGameService _gameService;



    public void OnGet()
    {

    }
}