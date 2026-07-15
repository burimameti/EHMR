using EHMR.Domain.Entities.Reports;
using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface IReportProvider
    {
        string Key
        {
            get;
        }

        string Title
        {
            get;
        }

        string Description
        {
            get;
        }

        string Icon
        {
            get;
        }
        event Action? FiltersChanged;
        ReportCategory Category
        {
            get;
        }

        ReportType Type
        {
            get;
        }

        IEnumerable<SparkGridColumn> Columns
        {
            get;
        }

        IEnumerable<SparkTabItem> BuildTabs();

        IEnumerable<SparkPickerItem> BuildPickers();

        IEnumerable<SparkButtonItem> BuildButtons();

        Task<List<DynamicReportRow>> GenerateAsync(
            DateTime from,
            DateTime to);

        ReportMetrics CalculateMetrics(
            IEnumerable<DynamicReportRow> rows);
    }

    public sealed class ReportMetrics
    {
        public string Title1 { get; set; } = string.Empty;
        public int Value1
        {
            get; set;
        }

        public string Title2 { get; set; } = string.Empty;
        public int Value2
        {
            get; set;
        }

        public string Title3 { get; set; } = string.Empty;
        public int Value3
        {
            get; set;
        }
    }

 

    public sealed class ReportMetric
    {

        public string Title
        {
            get; init;
        }
            = string.Empty;


        public string Value
        {
            get; init;
        }
            = string.Empty;


        public MetricTone Tone
        {
            get; init;
        }

    }


    public enum MetricTone
    {
        Neutral,
        Success,
        Warning,
        Danger
    }
}
