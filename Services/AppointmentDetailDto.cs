using EHMR.Domain.Entities;

namespace EHMR.Services
{
    public class AppointmentDetailDto
    {
        public Appointment Appointment { get; set; } = new();

        public List<AppointmentDiagnosis> Diagnoses { get; set; } = [];

        public List<TherapyCycle> TherapyCycles { get; set; } = [];

        public List<Patient> Patients { get; set; } = [];

        public List<Doctor> Doctors { get; set; } = [];

        public Appointment? PreviousAppointment
        {
            get; set;
        }

        public Appointment? NextAppointment
        {
            get; set;
        }

        public int TotalAppointments
        {
            get; set;
        }

        public int TotalDiagnoses
        {
            get; set;
        }

        public int TotalCycles
        {
            get; set;
        }
    }
}