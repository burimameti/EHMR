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
        public string DoctorName{get;set; }
        = string.Empty;
        public string? DoctorSurname { get; set; } = string.Empty;

        public User User { get; set; } = null!;

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
               
                return DoctorName + " " + DoctorSurname;
            }
        }

    }
   
    public enum Status
    {
        Active,
        Inactive
    }
}