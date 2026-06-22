namespace EHMR.Domain.Entities
{
    public class Prescription : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Patient Patient { get; set; } = null!;

        public Encounter? Encounter
        {
            get; set;
        }

        public Guid? EncounterId
        {
            get; set;
        }

        public string Dosage
        {
            get;
            set;
        }

        public string Medication
        {
            get;
            set;
        }

        public string Instructions
        {
            get;
            set;
        }
    }
}