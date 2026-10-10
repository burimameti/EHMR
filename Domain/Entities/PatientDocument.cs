using EHMR.Domain.Entities;
namespace EHMR.Domain.Entities;
public class PatientDocument : BaseEntity
{ 
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid PatientId
    {
        get; set;
    }
    public Patient? Patient
    {
        get; set;
    }

    public Guid? EncounterId
    {
        get; set;
    }
    public Encounter? Encounter
    {
        get; set;
    }
public PatientDocumentType DocumentType
    {
        get; set;
    }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string StoredPath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSize
    {
        get; set;
    }

    public bool IsCritical
    {
        get; set;
    }

    public bool IsDeleted
    {
        get; set;
    }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public enum PatientDocumentType
{
    Resenie,
    Referral,
    Laboratory,
    BloodAnalysis,
    XRay,
    Ultrasound,
    CT,
    MRI,
    ECG,
    EMG,
    DischargeLetter,
    SpecialistReport,
    Prescription,
    ConsentForm,
    TherapyPlan,
    Insurance,
    Other
}