namespace EHMR.Domain.Entities
{
    public class Encounter : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Patient Patient { get; set; } = null!;

        public DateTime Date
        {
            get; set;
        }

        public Guid DoctorId
        {
            get; set;
        }

        public Doctor Doctor { get; set; } = null!;

        public Guid? AppointmentId
        {
            get; set;
        }

        public DateTime PeriodStart
        {
            get; set;
        }

        public DateTime? PeriodEnd
        {
            get; set;
        }

        public string ChiefComplaint { get; set; } = string.Empty;
        public string ClinicalNotes { get; set; } = string.Empty;

        public string EncounterType { get; set; } = "Outpatient"; // Outpatient, Inpatient, Telehealth
        public string Status { get; set; } = "InProgress";
    }
}