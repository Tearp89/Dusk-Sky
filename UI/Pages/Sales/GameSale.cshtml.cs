using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

public class SaleDetailViewModel
{
    public string SaleId { get; set; }
    public string GameId { get; set; }
    public string GameTitle { get; set; }
    public string GameHeaderUrl { get; set; }
    public string GameDescription { get; set; }
    public float Price { get; set; }
}

public class PaymentInputModel
{
    public string SaleId { get; set; }
    public string GameId { get; set; }

    [Required(ErrorMessage = "El nombre en la tarjeta es requerido.")]
    [Display(Name = "Nombre en la Tarjeta")]
    public string CardName { get; set; }

    [Required(ErrorMessage = "El número de tarjeta es requerido.")]
    [CreditCard(ErrorMessage = "Número de tarjeta inválido.")]
    [Display(Name = "Número de Tarjeta")]
    public string CardNumber { get; set; }

    [Required(ErrorMessage = "La fecha de expiración es requerida.")]
    [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Formato debe ser MM/YY")]
    [Display(Name = "Fecha de Expiración (MM/YY)")]
    public string ExpiryDate { get; set; }

    [Required(ErrorMessage = "El CVC es requerido.")]
    [RegularExpression(@"^[0-9]{3,4}$", ErrorMessage = "CVC inválido.")]
    [Display(Name = "CVC")]
    public string Cvc { get; set; }
}

public class GameSaleModel : PageModel
{
    private readonly ISalesService _salesService;
    private readonly IGameService _gameService;

    public GameSaleModel(ISalesService salesService, IGameService gameService)
    {
        _salesService = salesService;
        _gameService = gameService;
    }

    public SaleDetailViewModel SaleInfo { get; set; }

    [BindProperty]
    public PaymentInputModel Input { get; set; }

    public async Task<IActionResult> OnGetAsync(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return NotFound();
        }

        var saleDetails = await _salesService.GetSaleByGameIdAsync(gameId);

        if (saleDetails == null)
        {
            return NotFound("Este juego no se encuentra en venta.");
        }

        var gameDetails = await _gameService.GetGamePreviewByIdAsync(Guid.Parse(gameId));
        if (gameDetails == null)
        {
            return NotFound("No se encontraron los detalles del juego.");
        }

        SaleInfo = new SaleDetailViewModel
        {
            SaleId = saleDetails.Id,
            GameId = gameDetails.Id.ToString(),
            GameTitle = gameDetails.Title,
            GameHeaderUrl = gameDetails.HeaderUrl,
            Price = saleDetails.Price
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {

        if (!ModelState.IsValid)
    {
        foreach (var modelStateKey in ViewData.ModelState.Keys)
        {
            var value = ViewData.ModelState[modelStateKey];
            foreach (var error in value.Errors)
            {
                Console.WriteLine($"Error de Validación - Campo: {modelStateKey}, Mensaje: {error.ErrorMessage}");
            }
        }

        await OnGetAsync(Input.GameId);
        return Page();
    }
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            ModelState.AddModelError(string.Empty, "No se pudo identificar al usuario. Por favor, inicia sesión de nuevo.");
            await OnGetAsync(Input.GameId);
            return Page();
        }

        var success = await _salesService.BuyGameAsync(Input.SaleId, userId);

        if (success)
        {
            TempData["SuccessMessage"] = "¡Compra realizada con éxito!";
            return RedirectToPage("/Users/MyGames");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "La compra no se pudo completar. Es posible que el juego ya no esté disponible.");
            await OnGetAsync(Input.GameId);
            return Page();
        }
    }
}