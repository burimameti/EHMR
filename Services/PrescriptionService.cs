using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore; // Доколку користите EF Core директно, во спротивно заменете со вашиот IRepository пат

namespace EHMR.Infrastructure.Services;

public class PrescriptionService : IPrescriptionService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public PrescriptionService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }


    public async Task<IEnumerable<Prescription>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Encounter)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Prescription?> GetByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Encounter)
            .FirstOrDefaultAsync(p => p.Id==id);
    }

    public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid patientId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Prescriptions
            .Include(p => p.Encounter)
            .Where(p => p.PatientId==patientId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<Prescription>> GetByEncounterIdAsync(Guid encounterId)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Where(p => p.EncounterId==encounterId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Prescription> CreateAsync(Prescription prescription)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if(prescription.Id==Guid.Empty)
        {
            prescription.Id=Guid.NewGuid();
        }

        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();

        return prescription;
    }

    public async Task<bool> UpdateAsync(Prescription prescription)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Prescriptions.Update(prescription);
        var affectedRows = await db.SaveChangesAsync();

        return affectedRows>0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var prescription = await db.Prescriptions.FindAsync(id);
        if(prescription==null)
            return false;

        db.Prescriptions.Remove(prescription);
        var affectedRows = await db.SaveChangesAsync();

        return affectedRows>0;
    }
}