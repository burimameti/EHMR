using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Reports;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Services;

public sealed class ReportHistoryService : IReportHistoryService
{

    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public ReportHistoryService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<ReportHistory> AddAsync(ReportHistory history)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        history.Id=Guid.NewGuid();
        history.GeneratedOn=DateTime.Now;
        history.Id=history.Id==Guid.Empty
                    ? Guid.NewGuid()
                    : history.Id;

        if(string.IsNullOrWhiteSpace(history.ReportNumber))
        {
            history.ReportNumber=
                await SequenceHelper.GenerateNumberAsync(_context, SequenceNames.Report, "REP");
        }
        _context.ReportHistories.Add(history);

        await _context.SaveChangesAsync();

        return history;
    }

    public async Task UpdateAsync(ReportHistory history)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        _context.ReportHistories.Update(history);

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        var entity = await _context.ReportHistories.FirstOrDefaultAsync(x => x.Id==id);

        if(entity==null)
            return;

        if(File.Exists(entity.FilePath))
        {
            File.Delete(entity.FilePath);
        }

        _context.ReportHistories.Remove(entity);

        await _context.SaveChangesAsync();
    }

    public async Task<ReportHistory?> GetAsync(Guid id)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id==id);
    }

    public async Task<List<ReportHistory>> GetAllAsync()
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .OrderByDescending(x => x.GeneratedOn)
            .ToListAsync();
    }

    public async Task<List<ReportHistory>> GetRecentAsync(int count = 50)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .OrderByDescending(x => x.GeneratedOn)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<ReportHistory>> GetByUserAsync(string userName)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .Where(x => x.GeneratedBy==userName)
            .OrderByDescending(x => x.GeneratedOn)
            .ToListAsync();
    }

    public async Task<List<ReportHistory>> GetByReportAsync(string reportKey)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .Where(x => x.ReportKey==reportKey)
            .OrderByDescending(x => x.GeneratedOn)
            .ToListAsync();
    }

    public async Task<List<ReportHistory>> GetBetweenAsync(DateTime from, DateTime to)
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories
            .AsNoTracking()
            .Where(x => x.GeneratedOn>=from&&
                        x.GeneratedOn<=to)
            .OrderByDescending(x => x.GeneratedOn)
            .ToListAsync();
    }

    public async Task<int> GetGeneratedTodayAsync()
    {
        await using var _context = await _factory.CreateDbContextAsync();
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        return await _context.ReportHistories.CountAsync(x =>
            x.GeneratedOn>=today&&
            x.GeneratedOn<tomorrow);
    }

    public async Task<long> GetStorageUsedAsync()
    {
        await using var _context = await _factory.CreateDbContextAsync();
        return await _context.ReportHistories.SumAsync(x => x.FileSize);
    }

    public async Task ClearAsync()
    {
        await using var _context = await _factory.CreateDbContextAsync();
        var reports = await _context.ReportHistories.ToListAsync();

        foreach(var report in reports)
        {
            if(File.Exists(report.FilePath))
            {
                File.Delete(report.FilePath);
            }
        }

        _context.ReportHistories.RemoveRange(reports);

        await _context.SaveChangesAsync();
    }
}