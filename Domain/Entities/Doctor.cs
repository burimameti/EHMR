using EHMR.Domain.Entities.Rbac;

namespace EHMR.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public string DoctorNumber { get; set; } = string.Empty;
        public Guid UserId
        {
            get; set;
        }

        public User User { get; set; } = null!;

        public string LicenseNumber { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public string? Email { get; set; } = string.Empty;

        public bool IsActive
        {
            get; set;
        }
        public Gender Gender
        {
            get; set;
        }
        public Status Status
        {
            get; set;
        }= Status.Active;
        // optional convenience
        public string FullName
        {
            get
            {
                if(User==null) return string.Empty;
                var name = $"{User.FirstName} {User.LastName}".Trim();
                return string.IsNullOrWhiteSpace(name) ? "Не е доделен" : name;
            }
        }

    }
   
    public enum Status
    {
        Active,
        Inactive,
        Suspended
    }
}