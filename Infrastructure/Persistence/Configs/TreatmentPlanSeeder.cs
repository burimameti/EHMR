using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class TreatmentPlanSeeder : IEntitySeeder
{
    public int Order => 20;

    public async Task SeedAsync(DesktopTherapyDbContext db, CancellationToken ct)
    {
        if(await db.TreatmentPlans.AnyAsync(ct))
            return;
        try
        {
            var plans = new List<TreatmentPlan>
        {
            new TreatmentPlan
            {
                Id = SeedIds.Plan1,

                PatientId = SeedIds.Patient1,
                DoctorId = SeedIds.Doctor1,
                TherapyProtocolId = Guid.Parse("11111111-1111-1111-1111-111111111111"),

                ProtocolName = "Ревматоиден Артритис - DMARD Терапија",
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = TherapyStatus.Active
            },

            new TreatmentPlan
            {
                Id = SeedIds.Plan2,
                PatientId = SeedIds.Patient2,
                DoctorId = SeedIds.Doctor2,
                TherapyProtocolId = Guid.Parse("C3333333-3333-3333-3333-333333333333"),
                ProtocolName = "Анкилозирачки Спондилитис",
                StartDate = DateTime.UtcNow.AddDays(-5),
                EndDate = DateTime.UtcNow.AddMonths(6),
                Status = TherapyStatus.Active
            }
        };
            await db.TreatmentPlans.AddRangeAsync(plans);
            await db.SaveChangesAsync();
        }
        catch(Exception)
        {
            throw;
        }
    }
}