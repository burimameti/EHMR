using EHMR.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

public class DiagnosisSeeder : IEntitySeeder
{
    public int Order => 54;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Set<Diagnosis>().AnyAsync(ct))
            return;

        var patients = await context.Set<Patient>().ToListAsync(ct);
        var appointments = await context.Set<Appointment>().ToListAsync(ct);

        if(patients.Count<10||appointments.Count<5)
            throw new InvalidOperationException("Not enough patients or appointments.");

        // =========================
        // REAL MKB CODES
        // =========================
        var cholera = await context.Mkb10Codes.FirstAsync(x => x.Code=="A00.0", ct);
        var shigella = await context.Mkb10Codes.FirstAsync(x => x.Code=="A03.0", ct);
        var tb = await context.Mkb10Codes.FirstAsync(x => x.Code=="A15.0", ct);
        var salmonella = await context.Mkb10Codes.FirstAsync(x => x.Code=="A02.0", ct);
        var giardia = await context.Mkb10Codes.FirstAsync(x => x.Code=="A07.1", ct);
        var rota = await context.Mkb10Codes.FirstAsync(x => x.Code=="A08.0", ct);

        var now = DateTime.UtcNow;

        var diagnoses = new List<Diagnosis>();

        // =========================
        // DIAGNOSES (NO RANDOM FK)
        // =========================

        var d1 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[0].Id,
            Mkb10CodeId=cholera.Id,
            DiagnosedAt=now.AddDays(-20),
            IsPrimary=true,
            Severity="Severe",
            ClinicalDescription="Клиничка слика на колера",
            Status=DiagnosisStatus.Active
        };

        var d2 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[1].Id,
            Mkb10CodeId=shigella.Id,
            DiagnosedAt=now.AddDays(-18),
            IsPrimary=true,
            Severity="Moderate",
            ClinicalDescription="Шигелоза со гастро симптоми",
            Status=DiagnosisStatus.Active
        };

        var d3 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[2].Id,
            Mkb10CodeId=tb.Id,
            DiagnosedAt=now.AddDays(-15),
            IsPrimary=true,
            Severity="Severe",
            ClinicalDescription="Туберкулоза сомнителна",
            Status=DiagnosisStatus.Chronic
        };

        var d4 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[3].Id,
            Mkb10CodeId=salmonella.Id,
            DiagnosedAt=now.AddDays(-12),
            IsPrimary=true,
            Severity="Moderate",
            ClinicalDescription="Салмонелна инфекција",
            Status=DiagnosisStatus.Active
        };

        var d5 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[4].Id,
            Mkb10CodeId=giardia.Id,
            DiagnosedAt=now.AddDays(-10),
            IsPrimary=true,
            Severity="Mild",
            ClinicalDescription="Гиардијаза",
            Status=DiagnosisStatus.InRemission
        };

        var d6 = new Diagnosis
        {
            Id=Guid.NewGuid(),
            PatientId=patients[5].Id,
            Mkb10CodeId=rota.Id,
            DiagnosedAt=now.AddDays(-8),
            IsPrimary=true,
            Severity="Moderate",
            ClinicalDescription="Ротавирусен ентерит",
            Status=DiagnosisStatus.Active
        };

        diagnoses.AddRange([d1, d2, d3, d4, d5, d6]);

        await context.Set<Diagnosis>().AddRangeAsync(diagnoses, ct);

        // =========================
        // APPOINTMENT DIAGNOSIS BRIDGE
        // =========================
        var appointmentDiagnoses = new List<AppointmentDiagnosis>
        {
            new()
            {
                AppointmentId = appointments[0].Id,
                Mkb10CodeId = cholera.Id,
                DiagnosisId = d1.Id
            },
            new()
            {
                AppointmentId = appointments[1].Id,
                Mkb10CodeId = shigella.Id,
                DiagnosisId = d2.Id
            },
            new()
            {
                AppointmentId = appointments[2].Id,
                Mkb10CodeId = tb.Id,
                DiagnosisId = d3.Id
            },
            new()
            {
                AppointmentId = appointments[3].Id,
                Mkb10CodeId = salmonella.Id,
                DiagnosisId = d4.Id
            },
            new()
            {
                AppointmentId = appointments[4].Id,
                Mkb10CodeId = giardia.Id,
                DiagnosisId = d5.Id
            }
        };

        await context.Set<AppointmentDiagnosis>()
            .AddRangeAsync(appointmentDiagnoses, ct);

        await context.SaveChangesAsync(ct);
    }
}