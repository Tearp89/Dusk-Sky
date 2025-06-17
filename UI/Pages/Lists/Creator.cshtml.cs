using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

public class CreateListModel : PageModel
{
    private readonly IGameListService _listService;

    public CreateListModel(IGameListService listService)
    {
        _listService = listService;
    }

    [BindProperty]
    public GameListDTO List { get; set; } = new();
    [BindProperty(SupportsGet = true)]
    public Guid? GameId { get; set; }


    [BindProperty]
    public string Tags { get; set; } = string.Empty;

    public async Task<IActionResult> OnPostAsync()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    List.UserId = userId;

    var listId = await _listService.CreateListAsync(List);
    if (string.IsNullOrEmpty(listId))
    {
        TempData["ErrorMessage"] = "Hubo un problema al crear la lista.";
        return Page();
    }

    return RedirectToPage("SelectGames", new { listId, gameId = GameId });
}

}