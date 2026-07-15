using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Reports;

namespace EHMR.Domain.Interfaces;

public interface IReportHistoryService
{
    Task<ReportHistory> AddAsync(ReportHistory history);

    Task UpdateAsync(ReportHistory history);

    Task DeleteAsync(Guid id);

    Task<ReportHistory?> GetAsync(Guid id);

    Task<List<ReportHistory>> GetAllAsync();

    Task<List<ReportHistory>> GetRecentAsync(int count = 50);

    Task<List<ReportHistory>> GetByUserAsync(string userName);

    Task<List<ReportHistory>> GetByReportAsync(string reportKey);

    Task<List<ReportHistory>> GetBetweenAsync(DateTime from, DateTime to);

    Task<int> GetGeneratedTodayAsync();

    Task<long> GetStorageUsedAsync();

    Task ClearAsync();
}