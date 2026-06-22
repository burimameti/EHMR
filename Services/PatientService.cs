using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services;

public class PatientService : IPatientService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public PatientService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Patients
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = new Patient { Id=id };
        db.Entry(entity).State=EntityState.Deleted;

        await db.SaveChangesAsync();
    }
}