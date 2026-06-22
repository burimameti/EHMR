using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class EncounterSeeder : IEntitySeeder
    {
        public int Order => 55;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.Encounters.AnyAsync(ct))
                return;

            var encounter = new Encounter
            {
                Id=SeedIds.Encounter1,

                AppointmentId=Guid.Parse("BE2C5155-23EE-4AA9-BE2D-2AACC953CD10"),
                PatientId=SeedIds.Patient2,
                DoctorId=SeedIds.Doctor1,

                EncounterDate=DateTime.UtcNow,

                Notes="Patient presented with stable vitals. No acute complications observed during examination.",

                CreatedAt=DateTime.UtcNow,
                UpdatedAt=DateTime.UtcNow
            };
            try
            {
                await context.Encounters.AddAsync(encounter, ct);
                await context.SaveChangesAsync(ct);
            }
            catch(Exception ex)
            {
                Console.Write($"Error Occured {ex.Message}");
            }
        }
    }
}