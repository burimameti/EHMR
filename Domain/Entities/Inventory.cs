namespace EHMR.Domain.Entities
{
    public class Inventory
    {
        public Guid Id
        {
            get; set;
        }

        // =========================
        // MEDICINE LINK
        // =========================
        public Guid MedicineId
        {
            get; set;
        }

        public Medicine Medicine
        {
            get; set;
        }

        // =========================
        // STOCK DATA
        // =========================
        public int InitialStock
        {
            get; set;
        }

        public int CurrentStock
        {
            get; set;
        }

        public int ReservedStock
        {
            get; set;
        } // optional: planned therapy usage

        public int MinimumStockLevel
        {
            get; set;
        }

        public int MaximumStockLevel
        {
            get; set;
        }

        // =========================
        // TRACKING
        // =========================
        public DateTime LastUpdated
        {
            get; set;
        }

        public InventoryStatus Status
        {
            get; set;
        }

        // =========================
        // TENANT
        // =========================
        public Guid TenantId
        {
            get; set;
        }

        public int MinimumStockAlert
        {
            get;
            set;
        }
    }

    public enum InventoryStatus
    {
        Normal = 0,
        LowStock = 1,
        OutOfStock = 2,
        OverStock = 3
    }
}