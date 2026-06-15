namespace EHMR.Domain.Entities
{
    public class CycleMedicationDose : BaseEntity
    {
        public Guid TherapyCycleId
        {
            get; set;
        }

        public TherapyCycle TherapyCycle { get; set; } = null!;

        public Guid MedicineId
        {
            get; set;
        }

        public Medicine Medicine { get; set; } = null!;

        // Ова ти е дефинирано во базата како NOT NULL
        public string TargetDosage { get; set; } = string.Empty;

        // Поправено: Смени од internal set во јавен set
        public decimal DosageValue
        {
            get; set;
        }

        public string DosageUnit
        {
            get; set;
        }

        // Внимавај: Имаш и PlannedDate и PlannedAdministrationDate.
        // Ќе ги изедначиме за да немаш NULL грешки.
        public DateTime PlannedDate
        {
            get; set;
        }

        public DateTime PlannedAdministrationDate
        {
            get; set;
        }

        public DateTime? AdministeredAt
        {
            get; set;
        }

        public Guid? AdministeredByNurseId
        {
            get; set;
        }

        public DoseStatus Status { get; set; } = DoseStatus.Delayed;
        public string ReasonIfSkipped { get; set; } = string.Empty;

        // Поправено: Смени од internal set во јавен set
        public int Quantity
        {
            get; set;
        }
    }
}