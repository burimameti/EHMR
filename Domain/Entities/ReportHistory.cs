using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class ReportHistory : BaseEntity
    {
        public Guid Id
        {
            get; set;
        }

        public string ReportKey { get; set; } = "";

        public string ReportTitle { get; set; } = "";

        public string Format { get; set; } = "";

        public string FileName { get; set; } = "";

        public string FilePath { get; set; } = "";

        public long FileSize
        {
            get; set;
        }

        public string? GeneratedBy
        {
            get; set;
        }

        public DateTime GeneratedOn
        {
            get; set;
        }

        public bool Success
        {
            get; set;
        }

        public string? MachineName
        {
            get; set;
        }

        public int DurationMs
        {
            get; set;
        }

        public DateTime? StartDate
        {
            get; set;
        }

        public DateTime? EndDate
        {
            get; set;
        }
    }
}
