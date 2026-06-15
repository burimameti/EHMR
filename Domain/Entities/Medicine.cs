namespace EHMR.Domain.Entities;
public class Medicine : BaseEntity {
    public string Name { get; set; } = string.Empty; 
    public string GenericName { get; set; } = string.Empty;
    public string DosageForm { get; set; } = string.Empty; 
    public string DefaultDosage { get; set; } = string.Empty;
}
