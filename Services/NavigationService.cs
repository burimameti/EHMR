using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using System.Diagnostics;

namespace EHMR.Services
{
    public class NavigationService : INavigationService
    {
        private readonly INavigationEvents _navEvents;

        private static readonly HashSet<string> RootRoutes =
        [
            AppRoutes.Dashboard,
            AppRoutes.CalendarPage,
            AppRoutes.Patients.List,
            AppRoutes.Encounters.List,
            AppRoutes.Appointments.List,
            AppRoutes.Protocols.List,
            AppRoutes.Medicines.List,
            AppRoutes.Reports.List,
            AppRoutes.Admin.AdminPanel,
            AppRoutes.Users.List,
            AppRoutes.Doctors.List,
            AppRoutes.Therapy.List,
            AppRoutes.Prescriptions.List,
            AppRoutes.Backup.Dashboard,
            AppRoutes.Backup.Backups,
            AppRoutes.Mkb10Codes.List
        ];

        public NavigationService(INavigationEvents navEvents)
        {
            _navEvents=navEvents;
        }

        private Page GetCurrentPage()
        {
            var window=Application.Current?.Windows?.FirstOrDefault();
            if(window==null||window.Page==null)
                throw new InvalidOperationException("No active window or page found.");

            return window.Page;
        }

        public async Task NavigateToAsync<TViewModel>(IDictionary<string, object>? parameters = null)
        {
            var route=typeof(TViewModel).Name.Replace("ViewModel", "Page");
            await GoToAsync(route, parameters);
        }

        public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if(string.IsNullOrWhiteSpace(route))
                throw new ArgumentNullException(nameof(route));

            try
            {
                var navParams=parameters??new Dictionary<string, object>();

                Shell? activeShell=Shell.Current??Application.Current?.MainPage as Shell;

                if(activeShell==null)
                {
                    Debug.WriteLine($"[Warning] Navigation requested before Shell initialization: {route}");
                    return;
                }

                var normalized=route.Trim('/');
                var target=RootRoutes.Contains(normalized)
                    ? $"//{normalized}"
                    : route;

                await activeShell.GoToAsync(target, true, navParams);

                _navEvents.NotifyRouteChanged(target);
                Debug.WriteLine($"Route called - {target} - {DateTime.Now}");
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Navigation failed for route [{route}]: {ex.Message}");
                throw;
            }
        }

        public async Task GoBackAsync()
        {
            if(Shell.Current?.Navigation?.NavigationStack?.Count>1)
            {
                await Shell.Current.Navigation.PopAsync();
            }
            else if(Shell.Current!=null)
            {
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                throw new InvalidOperationException("Shell.Current is null. Ensure your app uses Shell.");
            }
        }

        public async Task PushModalAsync(object page)
        {
            if(page is not Page p)
                throw new ArgumentException("Parameter must be of type Page", nameof(page));

            var currentPage=GetCurrentPage();
            await currentPage.Navigation.PushModalAsync(p);
        }

        public async Task<object?> PopAsync()
        {
            if(Shell.Current==null)
                throw new InvalidOperationException("Shell.Current is null. Ensure your app uses Shell.");

            return await Shell.Current.Navigation.PopAsync();
        }
    }
}
