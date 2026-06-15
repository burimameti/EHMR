using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Constants.AppRoutes;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class PrescriptionMedicineSeeder : IEntitySeeder
{
    public int Order => 23;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Set<PrescriptionMedicine>().AnyAsync(ct))
            return;

        var items = new List<PrescriptionMedicine>
        {
            new PrescriptionMedicine
            {
                PrescriptionId = SeedIds.Presc1,
                MedicineId = SeedIds.Med1,

                Dosage = "2.5 mg",
                Frequency = "2x дневно",
                DurationDays = 30
            }
        };
        await context.PrescriptionMedicines.AddRangeAsync(items);

        // IMPORTANT: save once per seeder (recommended)
        await context.SaveChangesAsync();
    }
}