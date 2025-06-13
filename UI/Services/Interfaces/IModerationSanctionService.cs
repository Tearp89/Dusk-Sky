public interface IModerationSanctionService
{
    Task<List<SanctionDTO>> GetAllAsync();
    Task<SanctionDTO?> GetByIdAsync(string id);
    Task<bool> CreateAsync(SanctionDTO sanction);
    Task<bool> UpdateAsync(string id, SanctionDTO sanction);
    Task<bool> DeleteAsync(string id);
}