using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

public class ScheduleMedicineRuleSeeder : IEntitySeeder
{
    public int Order => 35;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.ScheduleMedicineRules.AnyAsync(ct))
            return;

        var schedules = await context.TherapySchedules
            .ToListAsync(ct);

        if(!schedules.Any())
            return;

        var rules = new List<ScheduleMedicineRule>();

        foreach(var schedule in schedules)
        {
            // DEMO medicines assumption (replace with real MedicineSeeder IDs)
            var baseMedicines = new[]
            {
                new { MedicineId = Guid.Parse("30000000-0000-0000-0000-000000000001"), DosageForm="IV", Dosage = "75 mg", Day = 1, Qty = 1 },
                new { MedicineId = Guid.Parse("30000000-0000-0000-0000-000000000002"), DosageForm="Tablet", Dosage = "2.5 mg", Day = 3, Qty = 2 },
            };

            foreach(var m in baseMedicines)
            {
                rules.Add(new ScheduleMedicineRule
                {
                    Id=Guid.NewGuid(),
                    TherapyScheduleId=schedule.Id,
                    MedicineId=m.MedicineId,

                    Dosage=m.Dosage,
                    AdministrationDayOffset=1,
                    Quantity=1
                });
            }
        }

        await context.ScheduleMedicineRules.AddRangeAsync(rules, ct);
        await context.SaveChangesAsync(ct);
    }
}