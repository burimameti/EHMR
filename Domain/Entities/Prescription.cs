namespace EHMR.Domain.Entities
{
    public class Prescription : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Patient Patient { get; set; } = null!;

        public List<PrescriptionMedicine> Medicines { get; set; } = new();

        public string Dosage
        {
            get;
            internal set;
        }

        public string Medication
        {
            get;
            internal set;
        }

        public string Instructions
        {
            get;
            internal set;
        }
    }
}