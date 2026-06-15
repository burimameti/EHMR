using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public interface INavigationHost
    {
        void SetPage(View view);

        void GoBack();
    }
}