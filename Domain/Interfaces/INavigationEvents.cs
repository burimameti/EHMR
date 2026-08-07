using System;

namespace EHMR.Domain.Interfaces
{
    public interface INavigationEvents
    {
        event EventHandler<string>? RouteChanged;

        void NotifyRouteChanged(string route);
    }
}