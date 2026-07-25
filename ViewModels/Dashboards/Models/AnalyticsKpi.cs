namespace EHMR.ViewModels.Dashboards.Models
{
    public class AnalyticsKpi
    {
        public int TotalPatients
        {
            get; set;
        }

        public int ActiveTherapies
        {
            get; set;
        }

        public int MissedCycles
        {
            get; set;
        }

        public int CriticalAlerts
        {
            get; set;
        }

        public double AdherenceRate
        {
            get; set;
        }
    }
}