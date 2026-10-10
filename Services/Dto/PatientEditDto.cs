using EHMR.Domain.Entities;

namespace EHMR.Services.Dto
{
    // =====================================================
    // PATIENT
    // =====================================================

    /// <summary>Read model — used for lists, selection, and as the source for PatientEditDto.</summary>
    public class PatientDto
    {
        public Guid Id

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? NationalId { get; set; }
        public string SzboNumber { get; set; } = "";
        public DateTime BirthDate

        public Gender Gender

        public Guid DoctorId

        public string DoctorDisplay { get; set; } = "";

        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string PostalCode { get; set; } = "";

        public string EmergencyContactName { get; set; } = "";
        public string EmergencyContactPhone { get; set; } = "";
        public string EmergencyRelationship { get; set; } = "";

        public string BloodType { get; set; } = "";
        public string Allergies { get; set; } = "";
        public PatientStatus Status

        public string InactiveReason { get; set; } = string.Empty;

        public DateTime RegistrationDate

        public int Age

        public DateTime? LastVisitDate

        public DateTime? NextAppointmentDate

        public List<DiagnosisDto> Diagnoses { get; set; } = [];
        public List<PatientMedicineDto> Medicines { get; set; } = [];
        public List<PatientDocumentDto> Documents { get; set; } = [];

        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>Write model bound directly to the edit form.</summary>
    public sealed class PatientEditDto
    {
        public Guid Id

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string SzboNumber { get; set; } = string.Empty;
        public DateTime BirthDate

        public Gender Gender

        public Guid DoctorId

        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        public string EmergencyContactName { get; set; } = string.Empty;
        public string EmergencyContactPhone { get; set; } = string.Empty;
        public string EmergencyRelationship { get; set; } = string.Empty;

        public string BloodType { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public PatientStatus Status

        public string InactiveReason { get; set; } = string.Empty;
    }

    // =====================================================
    // DIAGNOSIS
    // =====================================================

    public class DiagnosisDto
    {
        public Guid Id

        public Guid PatientId

        public Guid? EncounterId

        public Guid? Mkb10CodeId

        public string Mkb10Code { get; set; } = "";
        public string Mkb10Description { get; set; } = "";

        public DateTime DiagnosedAt

        public bool IsPrimary

        public string Severity { get; set; } = "";
        public string ClinicalDescription { get; set; } = "";
        public DiagnosisStatus Status

    }

    // =====================================================
    // PATIENT MEDICINE
    // =====================================================

    public class PatientMedicineDto
    {
        public Guid Id

        public Guid PatientId

        public Guid? EncounterId { get; set; }

        public Guid MedicineId

        public Guid? ApplicationRegimeId { get; set; }
        public string Dosage { get; set; } = "";

        public decimal Quantity { get; set; } = 1;
        public string GenericName { get; set; } = "";
        public string Code { get; set; } = "";
        public string DosageForm { get; set; } = "";
        public decimal Strength { get; set; }
        public string Unit { get; set; } = "";
        public string DefaultDosage { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public bool IsActive

    }

    // =====================================================
    // PATIENT DOCUMENT
    // =====================================================

    public class PatientDocumentDto
    {
        public Guid Id

        public Guid PatientId

        public Guid? EncounterId

        public PatientDocumentType DocumentType

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public string FileName { get; set; } = "";
        public string StoredPath { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long FileSize

        public bool IsCritical

        public DateTime UploadedAt

    }

    // =====================================================
    // MEDICINE (catalog search results)
    // =====================================================

    public class MedicineDto
    {
        public Guid Id

        public string Name { get; set; } = "";
        public string GenericName { get; set; } = "";
        public string Code { get; set; } = "";
        public string DosageForm { get; set; } = "";
        public decimal Strength

        public string Unit { get; set; } = "";
        public string DefaultDosage { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public bool IsActive

        public string FullName => Strength==0||string.IsNullOrWhiteSpace(Unit)
            ? Name
            : $"{Name} {Strength}{Unit}".Trim();
    }

    // =====================================================
    // MKB10 (search results)
    // =====================================================

    public class Mkb10CodeDto
    {
        public Guid Id

        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
    }

    // =====================================================
    // DOCTOR (search results)
    // =====================================================

    public class DoctorDto
    {
        public Guid Id

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DisplayName => $"Д-р {FirstName} {LastName}";
    }
    public class ApplicationRegimeDto
    {
        public Guid Id { get; set; }
        public string Regime { get; set; } = "";
        public bool IsActive { get; set; }
        public string Display => Regime;
    }

}