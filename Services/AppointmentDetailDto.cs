using EHMR.Domain.Entities;

namespace EHMR.Services
{
    public class AppointmentDetailDto
    {
        public Appointment Appointment { get; set; } = new();
        public Encounter LinkedEncounter { get; set; } = new();
        public List<Diagnosis> Mkb10Assignments { get; set; } = [];

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

        public int TotalMkb10Assignments
        {
            get; set;
        }}
}