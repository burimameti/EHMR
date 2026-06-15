using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities.Reports
{
    public enum ReportType
    {
        MissedTherapies,
        Auditing,
        AppointmentStatuses,
        Patients
    }

    public enum ReportCategory
    {
        Clinical,
        Operational,
        SecurityAuditing, MissedTherapies,
        Auditing,
        AppointmentStatuses,
        Patients
    }

    public class ReportDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "📊";

        public ReportCategory Category
        {
            get; set;
        }

        public string RolesRequired { get; set; } = "Admin, Doctor, Operator";
    }

    public class DynamicReportColumn
    {
        public string HeaderName { get; set; } = string.Empty;
        public string Width { get; set; } = "*";
    }

    public class DynamicReportRow
    {
        public List<string> Cells { get; set; } = new();

        public bool IsAlertSeverity
        {
            get; set;
        }
    }
}