namespace EHMR.Domain.Entities
{
    public class Diagnosis : BaseEntity
    {
        public Guid PatientId
        {
            get; set;
        }

        public Guid EncounterId
        {
            get; set;
        } // Every valid diagnosis stems from an clinical encounter event

        public string ClinicalDescription { get; set; } = string.Empty;

        // Enterprise Interoperability Mapping Codes
        public string Code { get; set; } = string.Empty; // e.g., "M54.5"

        public string System { get; set; } = "ICD-10";   // e.g., "ICD-10", "SNOMED-CT"

        public string Severity { get; set; } = "Moderate"; // Mild, Moderate, Severe
        public bool IsPrimary { get; set; } = false;
        public DateTime DiagnosedAt { get; set; } = DateTime.UtcNow;
    }
}