namespace EHMR.Domain.Entities
{
    public class PrescriptionMedicine
    {
        public Guid PrescriptionId
        {
            get; set;
        }

        public Prescription Prescription { get; set; } = null!;

        public Guid MedicineId
        {
            get; set;
        }

        public Medicine Medicine { get; set; } = null!;

        // clinical details per medicine
        public string Dosage { get; set; } = string.Empty;

        public string Frequency { get; set; } = string.Empty;

        public int DurationDays
        {
            get; set;
        }
    }
}