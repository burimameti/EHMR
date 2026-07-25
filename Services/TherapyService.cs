using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services;

public interface ITherapyService
{
    Task<List<TherapyCycle>> GetCyclesAsync();

    Task AddCycleAsync(TherapyCycle cycle);

    Task UpdateCycleAsync(TherapyCycle cycle);

    Task DeleteCycleAsync(Guid id);
}

public class TherapyService : ITherapyService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    public TherapyService(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        _dbFactory=dbFactory;
    }

    public async Task<List<TherapyCycle>> GetCyclesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        return await db.TherapyCycles
            .Include(x => x.Patient)
            .Include(x => x.Appointments)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
    }

    public async Task AddCycleAsync(TherapyCycle cycle)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        if(string.IsNullOrWhiteSpace(cycle.TherapyCyleNumber))
        {
            cycle.TherapyCyleNumber=
                await SequenceHelper.GenerateNumberAsync(db, SequenceNames.TherapyCycle, "TER");
        }
        db.TherapyCycles.Add(cycle);
        await db.SaveChangesAsync();
    }

    public async Task UpdateCycleAsync(TherapyCycle cycle)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.TherapyCycles.Update(cycle);
        await db.SaveChangesAsync();
    }

    public async Task DeleteCycleAsync(Guid id)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var entity = new TherapyCycle { Id=id };
        db.Entry(entity).State=EntityState.Deleted;

        await db.SaveChangesAsync();
    }
}