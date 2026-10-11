using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Demo MKB-10 assignments using the new patient-card / encounter scope.
/// Patient-card rows have no EncounterId; encounter rows belong only to that visit.
/// The seeder resolves catalog IDs by code so it does not replace or hard-code the
/// imported MKB-10 catalog identifiers.
/// </summary>
public sealed class PatientMkb10AssignmentSeeder : IEntitySeeder
{
    public int Order => 58;

    private sealed record AssignmentSeed(
        Guid Id,
        string AssignmentNumber,
        Guid PatientId,
        Guid? EncounterId,
        string Code,
        int DaysOffset,
        bool IsPrimary,
        string Severity,
        string Description,
        PatientMkb10AssignmentStatus Status);

    private static readonly AssignmentSeed[] Seeds =
    [
        // Patient card: chronic diagnosis, independent of any individual visit.
        new(
            Guid.Parse("00000000-0000-0000-0000-000000009201"),
            "MKB-DEMO-P3-001",
            SeedIds.Patient3,
            null,
            "M06.9",
            -300,
            true,
            "Хронична",
            "Хронична реуматолошка состојба; се следи низ контролни прегледи.",
            PatientMkb10AssignmentStatus.Chronic),

        new(
            Guid.Parse("00000000-0000-0000-0000-000000009202"),
            "MKB-DEMO-P1-001",
            SeedIds.Patient1,
            null,
            "I10",
            -90,
            true,
            "Умерена",
            "Есенцијална хипертензија.",
            PatientMkb10AssignmentStatus.Active),

        // Visit-level diagnoses: each belongs to its specific encounter.
        new(
            Guid.Parse("00000000-0000-0000-0000-000000009211"),
            "MKB-DEMO-E8001",
            SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008001"),
            "M06.9",
            -240,
            true,
            "Висока",
            "Почетна проценка на воспалителна болка во зглобовите.",
            PatientMkb10AssignmentStatus.Active),

        new(
            Guid.Parse("00000000-0000-0000-0000-000000009212"),
            "MKB-DEMO-E8002",
            SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008002"),
            "M06.9",
            -150,
            true,
            "Умерена",
            "Контролен преглед по воведување терапија.",
            PatientMkb10AssignmentStatus.Active),

        new(
            Guid.Parse("00000000-0000-0000-0000-000000009213"),
            "MKB-DEMO-E8003",
            SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008003"),
            "M06.9",
            -60,
            true,
            "Умерена",
            "Повторна проценка на хроничната состојба.",
            PatientMkb10AssignmentStatus.Chronic),

        new(
            Guid.Parse("00000000-0000-0000-0000-000000009214"),
            "MKB-DEMO-E8004",
            SeedIds.Patient3,
            Guid.Parse("00000000-0000-0000-0000-000000008004"),
            "M06.9",
            -14,
            true,
            "Ниска",
            "Контролен преглед со подобрен клинички одговор.",
            PatientMkb10AssignmentStatus.Chronic),

        new(
            Guid.Parse("00000000-0000-0000-0000-000000009215"),
            "MKB-DEMO-E1",
            SeedIds.Patient1,
            SeedIds.Encounter1,
            "I10",
            -90,
            true,
            "Умерена",
            "Контрола на крвен притисок.",
            PatientMkb10AssignmentStatus.Active)
    ];

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        var codesNeeded = Seeds.Select(x => x.Code).Distinct().ToArray();
        var codeIds = await context.Mkb10Codes
            .Where(x => codesNeeded.Contains(x.Code))
            .Select(x => new { x.Code, x.Id })
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, ct);

        if (codeIds.Count == 0)
        {
            Console.WriteLine("[MKB10] Каталогот нема соодветни шифри; MKB assignment demo seed е прескокнат.");
            return;
        }

        var seedIds = Seeds.Select(x => x.Id).ToArray();
        var existingIds = await context.PatientMkb10Assignments
            .Where(x => seedIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var existingAssignmentNumbers = await context.PatientMkb10Assignments
            .Where(x => Seeds.Select(s => s.AssignmentNumber).Contains(x.AssignmentNumber))
            .Select(x => x.AssignmentNumber)
            .ToListAsync(ct);

        var rows = Seeds
            .Where(x => codeIds.ContainsKey(x.Code))
            .Where(x => !existingIds.Contains(x.Id) && !existingAssignmentNumbers.Contains(x.AssignmentNumber))
            .Select(x => new PatientMkb10Assignment
            {
                Id = x.Id,
                AssignmentNumber = x.AssignmentNumber,
                PatientId = x.PatientId,
                EncounterId = x.EncounterId,
                Mkb10CodeId = codeIds[x.Code],
                DiagnosedAt = DateTime.UtcNow.Date.AddDays(x.DaysOffset),
                IsPrimary = x.IsPrimary,
                Severity = x.Severity,
                ClinicalDescription = x.Description,
                Status = x.Status,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "Seed"
            })
            .ToList();

        if (rows.Count == 0)
            return;

        await context.PatientMkb10Assignments.AddRangeAsync(rows, ct);
        await context.SaveChangesAsync(ct);
    }
}
