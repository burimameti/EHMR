using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class InventoryTransaction : BaseEntity
    {
        public Guid InventoryId
        {
            get; set;
        }

        public Inventory Inventory { get; set; } = null!;

        public Guid MedicineId
        {
            get; set;
        }

        public Medicine Medicine { get; set; } = null!;

        public Guid? TherapyDoseId
        {
            get; set;
        }

        public CycleMedicationDose? TherapyDose
        {
            get; set;
        }

        public Guid? TherapyCycleId
        {
            get; set;
        }

        public TherapyCycle? TherapyCycle
        {
            get; set;
        }

        public Guid? PatientId
        {
            get; set;
        }

        public Patient? Patient
        {
            get; set;
        }

        public InventoryTransactionType TransactionType
        {
            get; set;
        }

        public decimal Quantity
        {
            get; set;
        }

        public DateTime TransactionDate
        {
            get; set;
        }

        public string Notes { get; set; } = string.Empty;

        public string? ReferenceNote
        {
            get;
            internal set;
        }
    }

    public enum InventoryTransactionType
    {
        StockIn = 1,
        Reserved = 2,
        Released = 3,
        Consumed = 4,
        Adjustment = 5
    }
}