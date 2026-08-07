using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Domain.Interfaces
{
    public interface INavigationHost
    {
        void SetPage(View view);

        void GoBack();
    }
}