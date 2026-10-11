using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Removes only legacy demo patients 6–20 and their dependent clinical records.
/// Real patients and non-seeded appointments are left untouched.
/// </summary>
public sealed class DemoPatientCleanupSeeder : IEntitySeeder
{
    public int Order => 999;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var obsoletePatientIds = new[]
        {
            SeedIds.Patient6, SeedIds.Patient7, SeedIds.Patient8, SeedIds.Patient9,
            SeedIds.Patient10, SeedIds.Patient11, SeedIds.Patient12, SeedIds.Patient13,
            SeedIds.Patient14, SeedIds.Patient15, SeedIds.Patient16, SeedIds.Patient17,
            SeedIds.Patient18, SeedIds.Patient19, SeedIds.Patient20
        };

        var obsoletePatients = await context.Patients
            .IgnoreQueryFilters()
            .Where(x => obsoletePatientIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);
        if (obsoletePatients.Count == 0)
            return;

        var encounterIds = await context.Encounters
            .Where(x => obsoletePatients.Contains(x.PatientId))
            .Select(x => x.Id)
            .ToListAsync(ct);

        // Remove dependants with non-cascading encounter foreign keys first.
        var medicines = await context.PatientMedicines
            .Where(x => obsoletePatients.Contains(x.PatientId)
                || (x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value)))
            .ToListAsync(ct);
        context.PatientMedicines.RemoveRange(medicines);

        var mkb = await context.PatientMkb10Assignments
            .Where(x => obsoletePatients.Contains(x.PatientId)
                || (x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value)))
            .ToListAsync(ct);
        context.PatientMkb10Assignments.RemoveRange(mkb);

        var scores = await context.PatientScores
            .Where(x => obsoletePatients.Contains(x.PatientId)
                || (x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value)))
            .ToListAsync(ct);
        context.PatientScores.RemoveRange(scores);

        var prescriptions = await context.Prescriptions
            .Where(x => obsoletePatients.Contains(x.PatientId)
                || (x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value)))
            .ToListAsync(ct);
        context.Prescriptions.RemoveRange(prescriptions);

        var documents = await context.PatientDocuments
            .Where(x => obsoletePatients.Contains(x.PatientId)
                || (x.EncounterId.HasValue && encounterIds.Contains(x.EncounterId.Value)))
            .ToListAsync(ct);
        context.PatientDocuments.RemoveRange(documents);

        await context.SaveChangesAsync(ct);

        var encounters = await context.Encounters
            .Where(x => encounterIds.Contains(x.Id))
            .ToListAsync(ct);
        context.Encounters.RemoveRange(encounters);

        var appointments = await context.Appointments
            .Where(x => obsoletePatients.Contains(x.PatientId))
            .ToListAsync(ct);
        context.Appointments.RemoveRange(appointments);

        await context.SaveChangesAsync(ct);

        var patients = await context.Patients
            .IgnoreQueryFilters()
            .Where(x => obsoletePatients.Contains(x.Id))
            .ToListAsync(ct);
        context.Patients.RemoveRange(patients);
        await context.SaveChangesAsync(ct);
    }
}
