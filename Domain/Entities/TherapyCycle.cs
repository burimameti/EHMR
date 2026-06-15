namespace EHMR.Domain.Entities
{
    public class TherapyCycle : BaseEntity
    {
        public Guid TherapyScheduleId
        {
            get; set;
        }

        public TherapySchedule? TherapySchedule
        {
            get; set;
        }

        public int CycleNumber
        {
            get; set;
        } // Sequence tracker: 1, 2, 3...

        public DateTime PlannedStartDate
        {
            get; set;
        }

        public DateTime PlannedEndDate
        {
            get; set;
        }

        public DateTime? StatusChangedAt
        {
            get; set;
        }

        public string? ReasonForMissing
        {
            get; set;
        } // Причина: „Алергиска реакција“, „Не се појавил“

        public bool IsEscalatedToMissedGroup
        {
            get; set;
        }

        public TherapyStatus Status { get; set; } = TherapyStatus.Planned;
        public List<CycleMedicationDose> ScheduledDoses { get; set; } = new();
    }
}