using EHMR.Domain.Entities;

namespace EHMR.Services.Dto;

public sealed class PatientMkb10CodeSaveModel
{
    public Guid Id { get; set; }
    public Guid? Mkb10CodeId { get; set; }
    public DateTime DiagnosedAt { get; set; }
    public bool IsPrimary { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string? ClinicalDescription { get; set; }
    public DiagnosisStatus Status { get; set; }
}

public sealed class PatientDocumentSaveModel
{
    public Guid Id { get; init; }
    public PatientDocumentType DocumentType { get; init; } = PatientDocumentType.Other;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string StoredPath { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; }
}

public sealed class PatientMedicineSaveModel
{
    public Guid Id { get; init; }
    public Guid MedicineId { get; init; }
    public string? Dosage { get; init; }
    public Guid? ApplicationRegimeId { get; init; }
    public decimal Quantity { get; init; }
    public bool IsActive { get; init; }
}
