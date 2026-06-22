using EHMR.Domain.Entities.Rbac;

namespace EHMR.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public Guid UserId
        {
            get; set;
        }

        public User User { get; set; } = null!;

        public string LicenseNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        public bool IsActive
        {
            get; set;
        }

        // optional convenience
        public string FullName => User.FirstName+" "+User.LastName;
    }
}