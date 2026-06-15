using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class MissedTherapyReportDto
    {
        public Guid PatientId
        {
            get; set;
        }

        public string PatientName { get; set; } = string.Empty;
        public string TherapyName { get; set; } = string.Empty;

        public int CycleNumber
        {
            get; set;
        }

        public DateTime MissedDate
        {
            get; set;
        }

        public string Reason { get; set; } = string.Empty;
    }
}