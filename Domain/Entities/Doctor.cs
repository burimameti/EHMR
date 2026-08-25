using EHMR.Domain.Entities.Rbac;
using System.ComponentModel.DataAnnotations.Schema;

namespace EHMR.Domain.Entities;

public class Doctor : BaseEntity
{
    // =========================================================
    // Identification
    // =========================================================

    public string DoctorNumber { get; set; } = string.Empty;

    // =========================================================
    // User relationship
    // =========================================================

    public Guid UserId
    {
        get; set;
    }

    public virtual User User { get; set; } = null!;

    // =========================================================
    // Contact
    // =========================================================

    public string ContactPhone { get; set; } = string.Empty;

    public string? Email
    {
        get; set;
    }

    // =========================================================
    // Doctor information
    // =========================================================

    public Gender Gender { get; set; } = Gender.Male;

    public Status Status { get; set; } = Status.Active;

    // =========================================================
    // Computed / UI convenience properties
    // =========================================================

    [NotMapped]
    public bool IsActive
    {
        get => Status==Status.Active;
        set => Status=value
            ? Status.Active
            : Status.Inactive;
    }

    [NotMapped]
    public string DoctorName =>
        User?.FirstName?.Trim()
        ??string.Empty;

    [NotMapped]
    public string DoctorSurname =>
        User?.LastName?.Trim()
        ??string.Empty;

    [NotMapped]
    public string FullName =>
        string.Join(
            " ",
            new[]
            {
                User?.FirstName?.Trim(),
                User?.LastName?.Trim()
            }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
}

public enum Status
{
    Active = 0,
    Inactive = 1
}