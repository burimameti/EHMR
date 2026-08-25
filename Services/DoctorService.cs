using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
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

    // ============================================================
    // GET ALL
    // ============================================================

    public async Task<List<Doctor>> GetAllAsync()
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.Doctors
            .AsNoTracking()
            .Include(x => x.User)
            .OrderBy(x => x.User.LastName)
            .ThenBy(x => x.User.FirstName)
            .ToListAsync();
    }

    // ============================================================
    // GET BY ID
    // ============================================================

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        return await db.Doctors
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id==id);
    }

    // ============================================================
    // ADD
    // ============================================================

    public async Task AddAsync(Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        await using var db =
            await _factory.CreateDbContextAsync();

        doctor.Id=
            doctor.Id==Guid.Empty
                ? Guid.NewGuid()
                : doctor.Id;

        if(doctor.User is null)
        {
            throw new InvalidOperationException(
                "Реуматологот мора да има поврзан корисник.");
        }

        // Ensure User has an ID.
        if(doctor.User.Id==Guid.Empty)
        {
            doctor.User.Id=Guid.NewGuid();
        }

        doctor.UserId=doctor.User.Id;

        NormalizeDoctor(doctor);

        if(string.IsNullOrWhiteSpace(doctor.DoctorNumber))
        {
            doctor.DoctorNumber=
                await SequenceHelper.GenerateNumberAsync(
                    db,
                    SequenceNames.Doctor,
                    "DOK");
        }

        // Add the graph explicitly.
        db.Doctors.Add(doctor);

        await db.SaveChangesAsync();
    }

    // ============================================================
    // UPDATE
    // ============================================================

    public async Task UpdateAsync(Doctor doctor)
    {
        ArgumentNullException.ThrowIfNull(doctor);

        await using var db =
            await _factory.CreateDbContextAsync();

        var existing =
            await db.Doctors
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id==doctor.Id);

        if(existing is null)
        {
            throw new InvalidOperationException(
                "Реуматологот не е пронајден.");
        }

        NormalizeDoctor(doctor);

        // --------------------------------------------------------
        // Doctor fields
        // --------------------------------------------------------

        existing.DoctorNumber=
            doctor.DoctorNumber;

        existing.ContactPhone=
            doctor.ContactPhone;

        existing.Email=
            doctor.Email;

        existing.Gender=
            doctor.Gender;

        existing.Status=
            doctor.Status;

        // --------------------------------------------------------
        // User fields
        // --------------------------------------------------------

        if(doctor.User is not null)
        {
            existing.User??=new User
            {
                Id=
                    doctor.User.Id==Guid.Empty
                        ? Guid.NewGuid()
                        : doctor.User.Id
            };

            existing.User.FirstName=
                doctor.User.FirstName?.Trim()
                ??string.Empty;

            existing.User.LastName=
                doctor.User.LastName?.Trim()
                ??string.Empty;

            // Ако User ги има овие properties во твојот model,
            // можеш да ги синхронизираш и нив:
            //
            // existing.User.Email = doctor.User.Email;
            // existing.User.PhoneNumber = doctor.User.PhoneNumber;
        }

        await db.SaveChangesAsync();
    }

    // ============================================================
    // DELETE
    // ============================================================

    public async Task DeleteAsync(Guid id)
    {
        await using var db =
            await _factory.CreateDbContextAsync();

        var doctor =
            await db.Doctors
                .FirstOrDefaultAsync(x => x.Id==id);

        if(doctor is null)
            return;

        db.Doctors.Remove(doctor);

        await db.SaveChangesAsync();
    }

    // ============================================================
    // NORMALIZATION
    // ============================================================

    private static void NormalizeDoctor(Doctor doctor)
    {
        doctor.ContactPhone=
            doctor.ContactPhone?.Trim()
            ??string.Empty;

        doctor.Email=
            string.IsNullOrWhiteSpace(doctor.Email)
                ? null
                : doctor.Email.Trim();

        if(doctor.User is not null)
        {
            doctor.User.FirstName=
                doctor.User.FirstName?.Trim()
                ??string.Empty;

            doctor.User.LastName=
                doctor.User.LastName?.Trim()
                ??string.Empty;
        }
    }
}