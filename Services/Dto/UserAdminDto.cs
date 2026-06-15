using EHMR.Domain.Entities;

namespace EHMR.Services
{
    public class UserAdminDto
    {
        public Guid Id
        {
            get; set;
        }

        public string Username { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public UserRole Role
        {
            get; set;
        }

        public UserPosition Position
        {
            get; set;
        }

        public bool IsActive
        {
            get; set;
        }

        public List<string> Modules { get; set; } = new();
    }
}