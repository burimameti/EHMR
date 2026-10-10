using System.ComponentModel.DataAnnotations;

namespace EHMR.Domain.Entities
{
    public static class SequenceNames
    {
        public const string Patient = "PATIENT";
        public const string Appointment = "APPOINTMENT";
        public const string PatientMkb10Assignment = "DIAGNOSIS";
        public const string Doctor = "DOCTOR";
        public const string Encounter = "ENCOUNTER";
        public const string Prescription = "PRESCRIPTION";
        public const string Therapy = "THERAPY";
        public const string Report = "REPORT";
        public const string Document = "DOCUMENT";
    }
    public class Sequence :BaseEntity
    {
        [MaxLength(50)]
        public string Name { get; set; } = default!;
   
        public DateOnly SequenceDate
        {
            get; set;
        }

        public int Value
        {
            get; set;
        }
    }
}