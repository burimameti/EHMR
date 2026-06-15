using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class TherapyScheduleSeeder : IEntitySeeder
{
    public int Order => 30;

    public async Task SeedAsync(DesktopTherapyDbContext db, CancellationToken ct = default)
    {
        if(await db.TherapySchedules.AnyAsync(ct))
            return;

        var baseDate = new DateTime(2026, 5, 1);

        var schedules = new List<TherapySchedule>
        {
            new TherapySchedule
            {
                Id = SeedIds.Schedule1,

                TreatmentPlanId = SeedIds.Plan1,

                Name = "Induction Cycle A - RA Protocol",
                FrequencyInDays = 21,
                GraceDays = 2,

                IsActive = true,

                StartDate = baseDate,
                EndDate = baseDate.AddMonths(6)
            },

            new TherapySchedule
            {
                Id = SeedIds.Schedule2,

                TreatmentPlanId = SeedIds.Plan2,

                Name = "Induction Cycle B - AS Protocol",
                FrequencyInDays = 31,
                GraceDays = 5,

                IsActive = true,

                StartDate = baseDate,
                EndDate = baseDate.AddMonths(3)
            }
        };
        await db.TherapySchedules.AddRangeAsync(schedules);
        await db.SaveChangesAsync();
    }
}