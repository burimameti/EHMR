using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Deterministic functional-coverage data for the desktop demo database.
/// This seeder intentionally creates combinations used to exercise filtering,
/// therapy/dose aggregation, application-regime selection, frequency selection,
/// patient status/gender/city, and medicine reports.
/// </summary>
public sealed class FunctionalCoverageSeeder : IEntitySeeder
{
    public int Order => 10;

    private static readonly Guid[] MedicineIds =
    [
        Guid.Parse("00000000-0000-0000-0000-00000000A001"),
        Guid.Parse("00000000-0000-0000-0000-00000000A002"),
        Guid.Parse("00000000-0000-0000-0000-00000000A003"),
        Guid.Parse("00000000-0000-0000-0000-00000000A004"),
        Guid.Parse("00000000-0000-0000-0000-00000000A005"),
        Guid.Parse("00000000-0000-0000-0000-00000000A006"),
        Guid.Parse("00000000-0000-0000-0000-00000000A007"),
        Guid.Parse("00000000-0000-0000-0000-00000000A008"),
        Guid.Parse("00000000-0000-0000-0000-00000000A009"),
        Guid.Parse("00000000-0000-0000-0000-00000000A010"),
        Guid.Parse("00000000-0000-0000-0000-00000000A011"),
        Guid.Parse("00000000-0000-0000-0000-00000000A012"),
        Guid.Parse("00000000-0000-0000-0000-00000000A013"),
        Guid.Parse("00000000-0000-0000-0000-00000000A014"),
        Guid.Parse("00000000-0000-0000-0000-00000000A015"),
        Guid.Parse("00000000-0000-0000-0000-00000000A016"),
        Guid.Parse("00000000-0000-0000-0000-00000000A017"),
        Guid.Parse("00000000-0000-0000-0000-00000000A018"),
        Guid.Parse("00000000-0000-0000-0000-00000000A019"),
        Guid.Parse("00000000-0000-0000-0000-00000000A020")
    ];

    private static readonly Guid[] PatientIds =
    [
        SeedIds.Patient1, SeedIds.Patient2, SeedIds.Patient3, SeedIds.Patient4,
        SeedIds.Patient5, SeedIds.Patient6, SeedIds.Patient7, SeedIds.Patient8,
        SeedIds.Patient9, SeedIds.Patient10, SeedIds.Patient11, SeedIds.Patient12,
        SeedIds.Patient13, SeedIds.Patient14, SeedIds.Patient15, SeedIds.Patient16,
        SeedIds.Patient17, SeedIds.Patient18, SeedIds.Patient19, SeedIds.Patient20
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var patients = await context.Patients
            .Where(x => PatientIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        if (patients.Count < PatientIds.Length)
            return;

        // Same-city coverage: several Skopje patients deliberately have
        // different gender/status combinations for list filters and reports.
        patients[SeedIds.Patient1].Gender = Gender.Male;
        patients[SeedIds.Patient1].Status = PatientStatus.Active;

        patients[SeedIds.Patient2].Gender = Gender.Female;
        patients[SeedIds.Patient2].Status = PatientStatus.Active;

        patients[SeedIds.Patient5].Gender = Gender.Male;
        patients[SeedIds.Patient5].Status = PatientStatus.Inactive;

        patients[SeedIds.Patient11].Gender = Gender.Female;
        patients[SeedIds.Patient11].Status = PatientStatus.Chronic;

        patients[SeedIds.Patient14].Gender = Gender.Male;
        patients[SeedIds.Patient14].Status = PatientStatus.Active;

        patients[SeedIds.Patient18].Gender = Gender.Male;
        patients[SeedIds.Patient18].Status = PatientStatus.Inactive;

        await context.SaveChangesAsync(ct);

        var regimes = await context.ApplicationRegimes
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Regime, x => x.Id, StringComparer.OrdinalIgnoreCase);

        var requiredRegimes = new[]
        {
            "Орално",
            "Поткожно",
            "Интравенски",
            "Интрамускулно",
            "Интраартикуларно",
            "Интраназално",
            "Топикално",
            "Сублингвално"
        };

        if (requiredRegimes.Any(x => !regimes.ContainsKey(x)))
            return;

        var medicines = await context.Medicines
            .Where(x => Enumerable.Range(1, 10)
                .Select(i => i == 1 ? SeedIds.Med1 :
                             i == 2 ? SeedIds.Med2 :
                             i == 3 ? SeedIds.Med3 :
                             i == 4 ? SeedIds.Med4 :
                             i == 5 ? SeedIds.Med5 :
                             i == 6 ? SeedIds.Med6 :
                             i == 7 ? SeedIds.Med7 :
                             i == 8 ? SeedIds.Med8 :
                             i == 9 ? SeedIds.Med9 : SeedIds.Med10)
                .Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (medicines.Count < 10)
            return;

        var existingIds = await context.PatientMedicines
            .Where(x => MedicineIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToHashSetAsync(ct);

        var rows = new List<PatientMedicine>
        {
            // SAME THERAPY / SAME MEDICINE / DIFFERENT DOSES.
            Coverage(0,  SeedIds.Patient1, SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Weekly,          "5 mg",  1, true),
            Coverage(1,  SeedIds.Patient2, SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Weekly,          "7.5 mg",2, true),
            Coverage(2,  SeedIds.Patient5, SeedIds.Med9, regimes["Поткожно"], DosesFrequency.EveryOtherDay,  "10 mg", 3, false),
            Coverage(3,  SeedIds.Patient11,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Monthly,       "15 mg", 4, true),
            Coverage(4,  SeedIds.Patient14,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Other,          "20 mg", 5, true),

            // Every supported application regime is represented.
            Coverage(5,  SeedIds.Patient6, SeedIds.Med1, regimes["Орално"],            DosesFrequency.Daily,          "1 таблета", 10, true),
            Coverage(6,  SeedIds.Patient7, SeedIds.Med2, regimes["Интравенски"],       DosesFrequency.Monthly,        "100 mg", 2, true),
            Coverage(7,  SeedIds.Patient8, SeedIds.Med3, regimes["Интрамускулно"],     DosesFrequency.EveryThreeDays,"50 mg",  3, true),
            Coverage(8,  SeedIds.Patient9, SeedIds.Med4, regimes["Интраартикуларно"],  DosesFrequency.Other,         "40 mg",  1, true),
            Coverage(9,  SeedIds.Patient10,SeedIds.Med5, regimes["Интраназално"],      DosesFrequency.Daily,         "2 впрскувања", 2, true),
            Coverage(10, SeedIds.Patient12,SeedIds.Med6, regimes["Топикално"],          DosesFrequency.Daily,         "1 апликација", 30, true),
            Coverage(11, SeedIds.Patient13,SeedIds.Med7, regimes["Сублингвално"],       DosesFrequency.TwiceDaily,    "1 таблета", 60, true),

            // More combinations for reporting/filter visibility.
            Coverage(12, SeedIds.Patient15,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Weekly, "7.5 mg", 2, true),
            Coverage(13, SeedIds.Patient16,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Monthly, "15 mg", 1, true),
            Coverage(14, SeedIds.Patient17,SeedIds.Med8, regimes["Орално"], DosesFrequency.ThreeTimesDaily, "1 таблета", 90, true),
            Coverage(15, SeedIds.Patient18,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Weekly, "10 mg", 3, false),
            Coverage(16, SeedIds.Patient19,SeedIds.Med10,regimes["Интравенски"], DosesFrequency.EveryOtherDay, "250 mg", 6, true),
            Coverage(17, SeedIds.Patient20,SeedIds.Med9, regimes["Поткожно"], DosesFrequency.Other, "20 mg", 2, true),

            // Additional frequency coverage.
            Coverage(18, SeedIds.Patient3, SeedIds.Med1, regimes["Орално"], DosesFrequency.EveryOtherDay, "1 таблета", 14, true),
            Coverage(19, SeedIds.Patient4, SeedIds.Med2, regimes["Орално"], DosesFrequency.ThreeTimesDaily, "1 таблета", 90, true)
        };

        var missing = rows.Where(x => !existingIds.Contains(x.Id)).ToList();
        if (missing.Count == 0)
            return;

        await context.PatientMedicines.AddRangeAsync(missing, ct);
        await context.SaveChangesAsync(ct);
    }

    private static PatientMedicine Coverage(
        int index,
        Guid patientId,
        Guid medicineId,
        Guid applicationRegimeId,
        DosesFrequency frequency,
        string dosage,
        decimal quantity,
        bool active)
        => new()
        {
            Id = MedicineIds[index],
            PatientId = patientId,
            MedicineId = medicineId,
            ApplicationRegimeId = applicationRegimeId,
            DosesFrequency = frequency,
            Dosage = dosage,
            Quantity = quantity,
    
           
            Notes = "Демо запис за функционално тестирање.",
            PharmaceuticalReference = $"COVERAGE-{index + 1:000}",
            IsActive = active
        };
}
