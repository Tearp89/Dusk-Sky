// Agrega esta nueva clase ViewModel al final del archivo o en un archivo separado.
using System.ComponentModel.DataAnnotations;

public class CreateSaleViewModel
{
    [Required]
    public string GameId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, 10000, ErrorMessage = "El precio debe ser mayor que cero.")]
    [Display(Name = "Precio")]
    public float Price { get; set; }

    [Required]
    [Range(1, 1000, ErrorMessage = "La cantidad debe ser al menos 1.")]
    [Display(Name = "Cantidad")]
    public int Quantity { get; set; }

    public string GameTitle { get; set; } = string.Empty;
}

public class GameSaleViewModel
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HeaderUrl { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Available { get; set; }
}

public class SaleSummaryWithGameDto
{
    public string GameId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string HeaderUrl { get; set; } = string.Empty;

    public float Price { get; set; }

    public int Sold { get; set; }

    public int Available { get; set; }
}

public class SaleViewModel
{
    public string GameId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HeaderUrl { get; set; } = string.Empty;
    public float Price { get; set; }
}