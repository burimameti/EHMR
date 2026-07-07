using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.SparkForm
{
    public enum SparkFormMode
    {
         Create,
        Edit,
        Detail,
        ReadOnly,
        Popup,
        Wizard,
        QuickCreate,
        InlineEdit,
        Search,
        Filter
    }
}
