namespace EHMR.Domain.Entities.Rbac
{
    public class UserScope : BaseEntity
    {
        public Guid UserId
        {
            get; set;
        }

        public User User { get; set; } = null!;

        public ScopeType ScopeType
        {
            get; set;
        }

        public Guid TargetId
        {
            get; set;
        } // HospitalId / DepartmentId

        public bool IsPrimary
        {
            get; set;
        }
    }
}