namespace EHMR.Domain.Entities
{
    public class PatientDocument
    {
        public Guid Id
        {
            get; set;
        }

        public Guid PatientId
        {
            get; set;
        }

        public Guid EncounterId
        {
            get; set;
        }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty; // Internal storage resolution pointer
        public string FileType { get; set; } = string.Empty;        // application/pdf, image/jpeg

        public long FileSizeInBytes
        {
            get; set;
        }

        public bool IsDeleted
        {
            get; set;
        }

        public bool IsCritical { get; set; } = false;

        public DateTime CreatedAt
        {
            get;
            set;
        }

        public DateTime UploadedAt
        {
            get;
            internal set;
        }
    }
}