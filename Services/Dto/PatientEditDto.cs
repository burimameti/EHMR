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
        {
            get; set;
        }

        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string? NationalId { get; set; }
        public string SzboNumber { get; set; } = "";
        public DateTime BirthDate
        {
            get; set;
        }
        public Gender Gender
        {
            get; set;
        }

        public Guid DoctorId
        {
            get; set;
        }
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
        {
            get; set;
        }
        public string InactiveReason { get; set; } = string.Empty;

        public DateTime RegistrationDate
        {
            get; set;
        }

        public int Age
        {
            get; set;
        }
        public DateTime? LastVisitDate
        {
            get; set;
        }
        public DateTime? NextAppointmentDate
        {
            get; set;
        }

        public List<DiagnosisDto> Diagnoses { get; set; } = [];
        public List<PatientMedicineDto> Medicines { get; set; } = [];
        public List<PatientDocumentDto> Documents { get; set; } = [];

        public string FullName => $"{FirstName} {LastName}";
    }

    /// <summary>Write model bound directly to the edit form.</summary>
    public sealed class PatientEditDto
    {
        public Guid Id
        {
            get; set;
        }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string SzboNumber { get; set; } = string.Empty;
        public DateTime BirthDate
        {
            get; set;
        }
        public Gender Gender
        {
            get; set;
        }

        public Guid DoctorId
        {
            get; set;
        }

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
        {
            get; set;
        }
        public string InactiveReason { get; set; } = string.Empty;
    }

    // =====================================================
    // DIAGNOSIS
    // =====================================================

    public class DiagnosisDto
    {
        public Guid Id
        {
            get; set;
        }
        public Guid PatientId
        {
            get; set;
        }
        public Guid? EncounterId
        {
            get; set;
        }

        public Guid? Mkb10CodeId
        {
            get; set;
        }
        public string Mkb10Code { get; set; } = "";
        public string Mkb10Description { get; set; } = "";

        public DateTime DiagnosedAt
        {
            get; set;
        }
        public bool IsPrimary
        {
            get; set;
        }
        public string Severity { get; set; } = "";
        public string ClinicalDescription { get; set; } = "";
        public DiagnosisStatus Status
        {
            get; set;
        }
    }

    // =====================================================
    // PATIENT MEDICINE
    // =====================================================

    public class PatientMedicineDto
    {
        public Guid Id
        {
            get; set;
        }
        public Guid PatientId
        {
            get; set;
        }

        public Guid? EncounterId { get; set; }

        public Guid MedicineId
        {
            get; set;
        }
        public Guid? ApplicationRegimeId { get; set; }
        public Guid? ResolutionDocumentId { get; set; }
        public string ApplicationRegime { get; set; } = "";
        public string MedicineName { get; set; } = "";

        public DosesFrequency DosesFrequency
        {
            get; set;
        }
        public string Dosage { get; set; } = "";
        public DateTime StartDate
        {
            get; set;
        }
        public DateTime? EndDate
        {
            get; set;
        }
        public string Notes { get; set; } = "";
        public string PharmaceuticalReference { get; set; } = "";
        public decimal Quantity { get; set; } = 1;
        public string GenericName { get; set; } = "";
        public string Code { get; set; } = "";
        public string DosageForm { get; set; } = "";
        public decimal Strength { get; set; }
        public string Unit { get; set; } = "";
        public string DefaultDosage { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public bool IsActive
        {
            get; set;
        }
    }

    // =====================================================
    // PATIENT DOCUMENT
    // =====================================================

    public class PatientDocumentDto
    {
        public Guid Id
        {
            get; set;
        }
        public Guid PatientId
        {
            get; set;
        }
        public Guid? EncounterId
        {
            get; set;
        }
        public Guid? TherapyCycleId
        {
            get; set;
        }

        public PatientDocumentType DocumentType
        {
            get; set;
        }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public string FileName { get; set; } = "";
        public string StoredPath { get; set; } = "";
        public string ContentType { get; set; } = "";
        public long FileSize
        {
            get; set;
        }

        public bool IsCritical
        {
            get; set;
        }
        public DateTime UploadedAt
        {
            get; set;
        }
    }

    // =====================================================
    // MEDICINE (catalog search results)
    // =====================================================

    public class MedicineDto
    {
        public Guid Id
        {
            get; set;
        }
        public string Name { get; set; } = "";
        public string GenericName { get; set; } = "";
        public string Code { get; set; } = "";
        public string DosageForm { get; set; } = "";
        public decimal Strength
        {
            get; set;
        }
        public string Unit { get; set; } = "";
        public string DefaultDosage { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public bool IsActive
        {
            get; set;
        }

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
        {
            get; set;
        }
        public string Code { get; set; } = "";
        public string Description { get; set; } = "";
    }

    // =====================================================
    // DOCTOR (search results)
    // =====================================================

    public class DoctorDto
    {
        public Guid Id
        {
            get; set;
        }
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