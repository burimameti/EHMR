using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

public class TherapyDoseSeeder : IEntitySeeder
{
    public int Order => 42;

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
            var rules = cycle.TherapySchedule?.MedicineRules;

            if(rules==null||!rules.Any())
                continue;

            foreach(var rule in rules)
            {
                var calculatedDate = cycle.PlannedStartDate.AddDays(rule.AdministrationDayOffset);

                // SAFE dosage handling
                string dosage = rule.Dosage??"0 mg";

                string[] parts = dosage.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                decimal value = 0;
                string unit = "mg";

                if(parts.Length>=1)
                    decimal.TryParse(parts[0], out value);

                if(parts.Length>=2)
                    unit=parts[1];

                doses.Add(new CycleMedicationDose
                {
                    Id=Guid.NewGuid(),

                    TherapyCycleId=cycle.Id,
                    MedicineId=rule.MedicineId,

                    Quantity=rule.Quantity,

                    DosageValue=value,
                    DosageUnit=unit,

                    TargetDosage=dosage,

                    PlannedDate=calculatedDate,
                    PlannedAdministrationDate=calculatedDate,

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