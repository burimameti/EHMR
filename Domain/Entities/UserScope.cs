namespace EHMR.Domain.Entities
{
    public class UserScope : BaseEntity
    {
        public Guid UserId
        {
            get; set;
        }

        public User User { get; set; } = null!;

        // Defines the data boundary type (e.g., "Hospital", "Ward", "Clinic")
        public string ScopeType { get; set; } = string.Empty;

        // The unique ID of the target data element (e.g., HospitalTenantId)
        public Guid TargetId
        {
            get; set;
        }

        public bool IsPrimaryScope { get; set; } = true;
    }
}