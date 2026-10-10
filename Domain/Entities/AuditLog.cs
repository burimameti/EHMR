namespace EHMR.Domain.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid PatientId;
        public string? PerformedBy;

        public Guid UserId
        {
            get; set;
        }

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string BeforeValue { get; set; } = string.Empty;

        public string? Data
        {
            get; set;
        }

        public string AfterValue { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now; 
public Guid EntityId
        {
            get;
            set;
        }
        public string? Description
        {
            get;
           set;
        }
    }
}