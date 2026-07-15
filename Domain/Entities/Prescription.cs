

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

        public string? Dosage
        {
            get;
            set;
        }

        public string? Medication
        {
            get;
            set;
        }

        public string Instructions
        {
            get;
            set;
        }

        public string Status { get; set; } = "Активни";
        public DateTime IssuedDate
        {
            get;
            internal set;
        }
        public DateTime ExpiryDate
        {
            get;
            internal set;
        }
        public string Notes
        {
            get;
            internal set;
        }
    }
}