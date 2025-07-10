using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISalesService
{
    Task<string?> CreateSaleAsync(SaleCreateDto sale);
    Task<bool> BuyGameAsync(string saleId, string userId);
    Task<List<SaleSummaryDto>> GetSalesSummaryAsync();
    Task<SaleDetailsDto?> GetSaleByGameIdAsync(string gameId); 
    Task<List<UserPurchaseDto>> GetPurchasesByUserAsync(string userId);
}
