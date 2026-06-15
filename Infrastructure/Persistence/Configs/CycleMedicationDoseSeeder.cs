using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public class CycleMedicationDoseSeeder : IEntitySeeder
{
    public int Order => 41;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.CycleMedicationDoses.AnyAsync(ct))
            return;

        var cycles = await context.TherapyCycles
            .Include(x => x.TherapySchedule)
                .ThenInclude(x => x.MedicineRules)
            .ToListAsync(ct);

        var doses = new List<CycleMedicationDose>();

        foreach(var cycle in cycles)
        {
            if(cycle.TherapySchedule?.MedicineRules==null)
                continue;

            foreach(var rule in cycle.TherapySchedule.MedicineRules)
            {
                var plannedDate = cycle.PlannedStartDate
                    .AddDays(rule.AdministrationDayOffset);

                // SAFE DOSAGE PARSING
                decimal dosageValue = 0;
                string dosageUnit = string.Empty;

                if(!string.IsNullOrWhiteSpace(rule.Dosage))
                {
                    var parts = rule.Dosage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if(parts.Length>0)
                        decimal.TryParse(parts[0], out dosageValue);

                    if(parts.Length>1)
                        dosageUnit=parts[1];
                }

                doses.Add(new CycleMedicationDose
                {
                    Id=Guid.NewGuid(),
                    TherapyCycleId=cycle.Id,
                    MedicineId=rule.MedicineId,

                    Quantity=rule.Quantity,

                    TargetDosage=rule.Dosage??"0 mg",
                    DosageValue=dosageValue,   // FIXED
                    DosageUnit=dosageUnit,  // enable if exists in entity

                    PlannedDate=plannedDate,
                    PlannedAdministrationDate=plannedDate,

                    Status=DoseStatus.Planned,
                    ReasonIfSkipped=string.Empty
                });
            }
        }

        if(doses.Count>0)
        {
            await context.CycleMedicationDoses.AddRangeAsync(doses, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}