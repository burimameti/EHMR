using EHMR.ViewModels.Dashboards.Models;

namespace EHMR.ViewModels
{
    public class AnalyticsDashboard
    {
        public AnalyticsKpi Kpis
        {
            get; set;
        }
            = new();

        public List<AnalyticsChart> AdherenceChart
        {
            get; set;
        }
            = new();

        public List<AnalyticsChart> TherapyChart
        {
            get; set;
        }
            = new();

        public List<AnalyticsChart> AlertChart
        {
            get; set;
        }
            = new();
    }
}