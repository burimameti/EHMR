using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs;

public class MedicineSeeder : IEntitySeeder
{
    public int Order => 11;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Medicines.AnyAsync(ct))
            return;

        var medicines = new List<Medicine>
        {
            new Medicine
            {
                Id = SeedIds.Med1,
                Name = "Цисплатин",
                GenericName = "Cisplatin",
                DosageForm = "IV",
                DefaultDosage = "75 mg/m2"
            },

            new Medicine
            {
                Id = SeedIds.Med2,
                Name = "Метотрексат",
                GenericName = "Methotrexate",
                DosageForm = "Tablet",
                DefaultDosage = "2.5 mg"
            }
        };

        await context.Medicines.AddRangeAsync(medicines, ct);
        await context.SaveChangesAsync(ct);
    }
}