namespace EHMR.ViewModels.Appointments
{
    public class DiagnosisHistoryItem
    {
        public Guid Id
        {
            get; set;
        }

        public string Code
        {
            get; set;
        }

        public string Description
        {
            get; set;
        }

        public DateTime DiagnosedAt
        {
            get; set;
        }

        public string Severity
        {
            get; set;
        }

        public bool IsPrimary
        {
            get; set;
        }
    }
}