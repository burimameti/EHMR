using EHMR.Domain.Interfaces;
using EHMR.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class NavigationHost : INavigationHost
    {
        public RootLayoutPage? Root
        {
            get; set;
        }

        public void SetPage(View view)
            => Root?.SetPage(view);

        public void GoBack()
            => Root?.GoBack();
    }
}