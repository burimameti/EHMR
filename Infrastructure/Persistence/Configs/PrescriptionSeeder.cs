using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Constants.AppRoutes;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class PrescriptionSeeder : IEntitySeeder
{
    public int Order => 22;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Prescriptions.AnyAsync(ct))
            return;

        var prescriptions = new List<Prescription>
        {
            new Prescription
            {
                Id = SeedIds.Presc1,
                PatientId = SeedIds.Patient1,
                Dosage= "1 таблетка дневно",
                Medication = "Парацетамол",
                Instructions = "При болка или температура над 38 градуса",
                UpdatedAt = DateTime.UtcNow,

                CreatedAt = DateTime.UtcNow
            }
        };
        await context.Prescriptions.AddRangeAsync(prescriptions);

        // IMPORTANT: save once per seeder (recommended)
        await context.SaveChangesAsync();
    }
}