namespace EHMR.Domain.Entities
{
    public class Encounter : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Patient? Patient
        {
            get; set;
        }

        public Guid AppointmentId
        {
            get; set;
        }

        public Appointment? Appointment
        {
            get; set;
        }

        public Guid DoctorId
        {
            get; set;
        }

        public Doctor? Doctor
        {
            get; set;
        }

        public DateTime EncounterDate
        {
            get; set;
        }

        public string Notes { get; set; } = "";

        public ICollection<Prescription> Prescriptions { get; set; } = [];

        public ICollection<Diagnosis> Diagnoses { get; set; } = [];
    }
}