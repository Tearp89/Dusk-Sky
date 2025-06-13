public interface IModerationReportService
{
    Task<List<ReportDTO>> GetAllAsync();
    Task<ReportDTO?> GetByIdAsync(string id);
    Task<bool> CreateAsync(ReportDTO report);
    Task<bool> UpdateAsync(string id, ReportDTO report);
    Task<bool> DeleteAsync(string id);
}