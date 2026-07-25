using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Dashboards.Models
{
    public class AnalyticsChart
    {
        public string Label { get; set; } = string.Empty;

        public double Value
        {
            get; set;
        }

        // NORMALIZED (0–1) for UI rendering
        public double NormalizedValue
        {
            get; set;
        }

        public Color Color { get; set; } = Colors.DodgerBlue;
    }
}