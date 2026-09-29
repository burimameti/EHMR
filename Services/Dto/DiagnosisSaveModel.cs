using EHMR.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services.Dto
{
    public class DiagnosisSaveModel
    {
        public Guid Id
        {
            get; set;
        }
        public Guid? Mkb10CodeId
        {
            get; set;
        }
        public Guid? EncounterId
        {
            get; set;
        }
        public DateTime DiagnosedAt
        {
            get; set;
        }
        public bool IsPrimary
        {
            get; set;
        }
        public string Severity
        {
            get; set;
        }
        public string? ClinicalDescription
        {
            get; set;
        }
        public DiagnosisStatus Status
        {
            get; set;
        }
    }


    public sealed class PatientDocumentSaveModel
    {
        public Guid Id
        {
            get; init;
        }

        public string FileName { get; init; } = string.Empty;

        public string StoredPath { get; init; } = string.Empty;

        public string ContentType { get; init; } = string.Empty;

        public DateTime UploadedAt
        {
            get; init;
        }
    }
    public sealed class PatientMedicineSaveModel
    {
        public Guid Id
        {
            get; init;
        }

        public Guid MedicineId
        {
            get; init;
        }

        public string? Dosage
        {
            get; init;
        }

        public DosesFrequency DosesFrequency
        {
            get; init;
        }

        public DateTime StartDate
        {
            get; init;
        }

        public DateTime? EndDate
        {
            get; init;
        }

        public string? Notes
        {
            get; init;
        }

        public string? PharmaceuticalReference
        {
            get; init;
        }

        public bool IsActive
        {
            get; init;
        }
    }
}
