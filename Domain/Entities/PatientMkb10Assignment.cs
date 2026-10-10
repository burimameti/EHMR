

namespace EHMR.Domain.Entities
{
    public class PatientMkb10Assignment : BaseEntity
    { 
        public string DiagnosisNumber { get; set; } = string.Empty;
        public Guid PatientId
        {
            get; set;
        }

        public Patient? Patient
        {
            get; set;
        }
        public Guid? EncounterId
        {
            get; set;
        }
        public Encounter? Encounter
        {
            get; set;
        }

        public Guid? Mkb10CodeId
        {
            get; set;
        }

        public Mkb10Code? Mkb10Code
        {
            get; set;
        }

        public DateTime DiagnosedAt
        {
            get; set;
        }

        public bool IsPrimary
        {
            get; set;
        }

        public string Severity { get; set; } = "";

        public string ClinicalDescription { get; set; } = "";

        public PatientMkb10AssignmentStatus Status
        {
            get; set;
        }
    }

    public enum PatientMkb10AssignmentStatus
    {
        Active = 1,          // тековна дијагноза
        Chronic = 2,         // хронична
        Resolved = 3,        // завршена / излекувана
        InRemission = 4,     // во ремисија
        Suspected = 5,       // сомневање
        RuledOut = 6,        // исклучена
        Inactive = 7         // неактивна
    }
}