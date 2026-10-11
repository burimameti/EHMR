using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Removes legacy seeded patients 6–20 and stale appointments/encounters attached to
/// the five fixed demo patients. Non-demo patients are never targeted.
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
        var demoPatientIds = new[] { SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4, SeedIds.Patient5 };
        var keepAppointmentIds = Enumerable.Range(1, 10)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{(7000 + i):D12}"))
            .ToArray();
        var keepEncounterIds = Enumerable.Range(1, 6)
            .Select(i => Guid.Parse($"00000000-0000-0000-0000-{(8000 + i):D12}"))
            .ToArray();

        var obsoletePatients = await context.Patients.IgnoreQueryFilters()
            .Where(x => obsoletePatientIds.Contains(x.Id))
            .Select(x => x.Id).ToListAsync(ct);

        var staleAppointments = await context.Appointments
            .Where(x => demoPatientIds.Contains(x.PatientId) && !keepAppointmentIds.Contains(x.Id))
            .ToListAsync(ct);
        var staleAppointmentIds = staleAppointments.Select(x => x.Id).ToArray();

        var staleEncounters = await context.Encounters
            .Where(x => (demoPatientIds.Contains(x.PatientId) && !keepEncounterIds.Contains(x.Id))
                || (x.AppointmentId.HasValue && staleAppointmentIds.Contains(x.AppointmentId.Value))
                || obsoletePatients.Contains(x.PatientId))
            .ToListAsync(ct);
        var encounterIds = staleEncounters.Select(x => x.Id).ToArray();

        if (obsoletePatients.Count == 0 && staleAppointments.Count == 0 && staleEncounters.Count == 0)
            return;

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

        context.Encounters.RemoveRange(staleEncounters);
        context.Appointments.RemoveRange(staleAppointments);
        await context.SaveChangesAsync(ct);

        var patients = await context.Patients.IgnoreQueryFilters()
            .Where(x => obsoletePatients.Contains(x.Id))
            .ToListAsync(ct);
        context.Patients.RemoveRange(patients);
        await context.SaveChangesAsync(ct);
    }
}
