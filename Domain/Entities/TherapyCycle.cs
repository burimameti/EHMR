namespace EHMR.Domain.Entities
{
    public class TherapyCycle : BaseEntity
    {
        public Guid? PatientId
        {
            get; set;
        }

        public Patient? Patient
        {
            get; set;
        }

        public int? CycleNumber
        {
            get; set;
        }

        public TherapyStatus? Status
        {
            get; set;
        }

        public DateTime? StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }

        public string? Notes { get; set; } = "";

        public ICollection<Appointment>? Appointments { get; set; } = [];
    }
}