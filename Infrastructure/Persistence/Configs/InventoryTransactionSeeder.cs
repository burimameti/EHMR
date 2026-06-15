using EHMR.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs
{
    // =====================================================
    // 7. INVENTORY FLOW
    // =====================================================

    public class InventoryTransactionSeeder : IEntitySeeder
    {
        public int Order => 70;

        public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
        {
            if(await context.InventoryTransactions.AnyAsync(ct))
                return;

            var inventories = await context.Inventories
                .AsNoTracking()
                .ToDictionaryAsync(x => x.MedicineId, ct);

            var doses = await context.CycleMedicationDoses
                .Include(x => x.TherapyCycle)
                .ThenInclude(x => x.TherapySchedule)
                .ThenInclude(x => x.TreatmentPlan)
                .ToListAsync(ct);

            var transactions = new List<InventoryTransaction>();

            foreach(var dose in doses)
            {
                if(!inventories.TryGetValue(dose.MedicineId, out var inv))
                    continue;

                var patientId = dose.TherapyCycle.TherapySchedule.TreatmentPlan.PatientId;

                transactions.Add(new InventoryTransaction
                {
                    Id=Guid.NewGuid(),
                    InventoryId=inv.Id,
                    MedicineId=dose.MedicineId,
                    TherapyDoseId=dose.Id,
                    TherapyCycleId=dose.TherapyCycleId,
                    PatientId=patientId,
                    Quantity=dose.Quantity,
                    TransactionType=InventoryTransactionType.Reserved,
                    TransactionDate=dose.PlannedAdministrationDate,
                    Notes=$"Reserved for cycle #{dose.TherapyCycle.CycleNumber}"
                });
            }

            context.InventoryTransactions.AddRange(transactions);
        }
    }
}