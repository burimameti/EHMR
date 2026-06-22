using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class TherapyProtocol : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string DiseaseCategory { get; set; } = string.Empty; // На пр. Онкологија, Кардиологија, Нефрологија...
        public string Description { get; set; } = string.Empty;
        public int DurationInDays { get; set; } = 0;

        public string CreatedByDoctor
        {
            get; set;
        }
    }
}