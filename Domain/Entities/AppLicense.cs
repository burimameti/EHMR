using System;
namespace EHMR.Domain.Entities;
public class AppLicense : BaseEntity {
    public string LicenseKey { get; set; } = string.Empty;
    public string LicensedTo { get; set; } = string.Empty; 
    public DateTime ValidUntil { get; set; }
    public int MaxAllowedPatients { get; set; } 
}
