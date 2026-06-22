using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class Mkb10Code : BaseEntity
    {
        /// <summary>The code itself, e.g. "M54.5".</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>Clinical description of the diagnosis, e.g. "Болка во долниот дел на грбот".</summary>
        public string Description
        {
            get; set;
        }

        /// <summary>Optional grouping/chapter from the official МКБ-10 classification.</summary>
        public string Chapter { get; set; } = string.Empty;

        /// <summary>Soft toggle so outdated codes can be hidden from the dropdown without deleting history.</summary>
        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string DisplayText => $"{Code} — {Description}";

        public List<AppointmentDiagnosis> AppointmentDiagnoses { get; set; } = new();
    }
}