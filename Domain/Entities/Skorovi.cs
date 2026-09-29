using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Entities
{
    public class Skorovi
    {
        public int Id { get; set;
        }
        public string Skore { get; set; } = string.Empty;
        public Guid PatientId { get; set;
        }
        public Patient Patient { get; set; } = null!;
    }
}
