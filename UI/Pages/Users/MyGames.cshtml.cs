using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

// ViewModel para mostrar cada juego comprado
public class PurchaseViewModel
{
    public string GameId { get; set; }
    public string GameTitle { get; set; }
    public string GameHeaderUrl { get; set; }
}

public class MyGamesModel : PageModel
{
    private readonly ISalesService _salesService;
    private readonly IGameService _gameService;

    public List<PurchaseViewModel> PurchasedGames { get; set; } = new();

    public MyGamesModel(ISalesService salesService, IGameService gameService)
    {
        _salesService = salesService;
        _gameService = gameService;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Account/Login");
        }

        var purchases = await _salesService.GetPurchasesByUserAsync(userId);
        
        if (purchases != null)
        {
            foreach (var purchase in purchases)
            {
                var gameDetails = await _gameService.GetGamePreviewByIdAsync(purchase.GameId);
                if (gameDetails != null)
                {
                    PurchasedGames.Add(new PurchaseViewModel
                    {
                        GameId = gameDetails.Id,
                        GameTitle = gameDetails.Title,
                        GameHeaderUrl = gameDetails.HeaderUrl
                    });
                }
            }
        }
        
        return Page();
    }
}