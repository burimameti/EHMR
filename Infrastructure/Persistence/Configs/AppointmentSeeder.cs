using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    // =====================================================
    // 6. CLINICAL OPERATIONS
    // =====================================================

    public class AppointmentSeeder : IEntitySeeder
    {
        public int Order => 50;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Set<Appointment>().AnyAsync(ct))
                return;

            await context.Set<Appointment>().AddRangeAsync(
                new Appointment
                {
                    PatientId=SeedIds.Patient1,DoctorId = SeedIds.Doctor1,
                    ScheduledStart=DateTime.UtcNow.AddDays(2),
                    ReasonForVisit="Initial consultation",
                    Status=AppointmentStatus.Scheduled
                }, new Appointment
                {
                    PatientId=SeedIds.Patient2,DoctorId=SeedIds.Doctor2,
                    ScheduledStart=DateTime.UtcNow.AddDays(3),
                    ReasonForVisit="Follow-up consultation",
                    Status=AppointmentStatus.Scheduled
                });

            await context.SaveChangesAsync();
        }
    }
}