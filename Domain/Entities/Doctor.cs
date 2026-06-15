namespace EHMR.Domain.Entities
{
    public class Doctor : BaseEntity
    {
        public Guid UserId
        {
            get; set;
        }

        public User User { get; set; } = null!;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string LicenseNumber { get; set; } = string.Empty;

        public string Specialty { get; set; } = string.Empty;

        public string ContactPhone { get; set; } = string.Empty;

        public bool IsActive
        {
            get; set;
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}