using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static EHMR.Infrastructure.Persistence.DesktopTherapyDbContext;

namespace EHMR.Infrastructure.Persistence.Configs
{
    public class InventorySeeder : IEntitySeeder
    {
        public int Order => 12;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            try
            {
                if(await context.Inventories.AnyAsync(ct))
                    return;

                context.Inventories.Add(
                    new Inventory
                    {
                        Id=SeedIds.Inventory1,
                        MedicineId=SeedIds.Med1,
                        InitialStock=500,
                        CurrentStock=500,
                        ReservedStock=0,
                        MinimumStockLevel=50,
                        MaximumStockLevel=1000,
                        Status=InventoryStatus.Normal,
                        LastUpdated=DateTime.UtcNow,
                        TenantId=SeedIds.Tenant
                    });

                await context.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                throw new Exception("Error seeding inventory", ex);
            }
        }
    }
}