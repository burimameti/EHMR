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

        public Guid PatientId
        {
            get; set;
        }

        public AlertLevel Level
        {
            get; set;
        }

        public bool IsRead
        {
            get;
            set;
        }

        public bool IsResolved
        {
            get;
            set;
        }
    }
}