namespace EHMR.Domain.Entities
{
    public enum AlertLevel
    {
        Info,
        Warning,
        Critical
    }

    public class Alert : BaseEntity
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stable identifier for the condition this alert represents
        /// (e.g. "StaleVisit:{patientId}", "OverdueCycle:{cycleId}").
        /// Used to skip re-creating an alert that's already open.
        /// </summary>
        public string DedupKey { get; set; } = string.Empty;

        public Guid PatientId
        {
            get; set;
        }
        public Patient? Patient
        {
            get; set;
        } // add if you went with the nav-property route from before

        public AlertLevel Level
        {
            get; set;
        }
        public bool IsRead
        {
            get; set;
        }
        public bool IsResolved
        {
            get; set;
        }
    }
}