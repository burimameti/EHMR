using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Services;

public class DoctorService : IDoctorService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public DoctorService(
        IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }

    public async Task<List<Doctor>> GetAllAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Doctors
            .Include(x => x.User)
            .AsNoTracking()
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        return await db.Doctors
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id==id);
    }

    public async Task AddAsync(Doctor doctor)
    {
        await using var db = await _factory.CreateDbContextAsync();
        doctor.Id=doctor.Id==Guid.Empty
                   ? Guid.NewGuid()
                   : doctor.Id;

        if(string.IsNullOrWhiteSpace(doctor.DoctorNumber))
        {
            doctor.DoctorNumber=
                await SequenceHelper.GenerateNumberAsync(db, SequenceNames.Doctor, "DOK");
        }
        db.Doctors.Add(doctor);

        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        await using var db = await _factory.CreateDbContextAsync();

        db.Doctors.Update(doctor);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var entity = new Doctor
        {
            Id=id
        };

        db.Entry(entity).State=EntityState.Deleted;

        await db.SaveChangesAsync();
    }
}