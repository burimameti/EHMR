using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class EncounterSeeder : IEntitySeeder
    {
        public int Order => 51;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Encounters.AnyAsync(ct))
                return;
            try
            {
                context.Encounters.Add(
               new Encounter
               {
                   Id=Guid.NewGuid(),
                   AppointmentId=SeedIds.Appt1,
                   PatientId=SeedIds.Patient1,
                   DoctorId=SeedIds.Doctor1,
                   Date=DateTime.UtcNow,
                   PeriodStart=SeedIds.Encounter1Start,
                   PeriodEnd=SeedIds.Encounter1End,
                   ClinicalNotes="Stable response",
                   CreatedAt=DateTime.Now,
                   UpdatedAt=DateTime.Now
               });
                await context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                throw new Exception("Expetionn", ex);
            }
        }
    }
}