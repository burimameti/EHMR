using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class NavigationEvents : INavigationEvents
    {
        public event EventHandler<string>? RouteChanged;

        public void NotifyRouteChanged(string route)
        {
            RouteChanged?.Invoke(this, route);
        }
    }
}