using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities.Rbac;
using System.Diagnostics;

namespace EHMR.Services
{
    public class NavigationService : INavigationService
    {
        private readonly INavigationEvents _navEvents;

        public NavigationService(INavigationEvents navEvents)
        {
            _navEvents=navEvents;
        }

        private Page GetCurrentPage()
        {
            var window = Application.Current?.Windows?.FirstOrDefault();
            if(window==null||window.Page==null)
                throw new InvalidOperationException("No active window or page found.");

            return window.Page;
        }

        public async Task NavigateToAsync<TViewModel>(IDictionary<string, object>? parameters = null)
        {
            var route = typeof(TViewModel).Name.Replace("ViewModel", "Page");
            await GoToAsync(route, parameters);
        }

        public async Task GoToAsync(string route, IDictionary<string, object>? parameters = null)
        {
            if(string.IsNullOrWhiteSpace(route))
                throw new ArgumentNullException(nameof(route));
            string normalizedRoute = "";
            try
            {
                var navParams = parameters??new Dictionary<string, object>();

                // 1. Прво земи безбедна референца за Shell (ако Current е null, пробај преку MainPage)
                Shell? activeShell = Shell.Current??Application.Current?.MainPage as Shell;

                if(activeShell==null)
                {
                    // Ако сè уште нема вчитано Shell, логирај предупредување и почекај малку (асинхрон fallback)
                    System.Diagnostics.Debug.WriteLine($"[Warning] Навигацијата за '{route}' е повикана прерано. Shell сè уште не е иницијализиран.");
                    return;
                }

                // Shell root pages must use absolute routes; registered detail/create/edit
                // pages remain relative so they stay inside the current Shell stack.
                 var cleanRoute = route.Split('?', 2)[0].Trim('/');
                normalizedRoute = cleanRoute switch
                {
                    "dashboard" => "//dashboard",
                    "login" => "//login",
                    _ when IsModuleRootRoute(cleanRoute) => $"//{cleanRoute}",
                    _ => route
                };

                // Menu navigation should be a single Shell operation. The previous
                // implementation performed PopToRootAsync + GoToAsync for module
                // routes, which made every menu click wait for two navigation
                // transitions. Disable Shell animation as well; page loading remains
                // owned by the destination ViewModel.
                await activeShell.GoToAsync(
                    normalizedRoute,
                    false,
                    navParams);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed за рута [{route}]: {ex.Message}");
                throw;
            }

            _navEvents.NotifyRouteChanged(normalizedRoute);
            Debug.WriteLine($"Route called - on navService{DateTime.Now}", route);
        }

        private static bool IsModuleRootRoute(string route)
        {
            var cleanRoute=route.Split('?', 2)[0].Trim('/');
            return cleanRoute.Equals(AppRoutes.Calendar, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.CalendarPage, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Patients.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Doctors.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Medicines.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.ApplicationRegimes.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Prescriptions.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Appointments.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Encounters.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Therapy.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Protocols.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Mkb10.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Mkb10Codes.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Reports.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Users.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Alerts.List, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Backup.Dashboard, StringComparison.OrdinalIgnoreCase)
                || cleanRoute.Equals(AppRoutes.Admin.AdminPanel, StringComparison.OrdinalIgnoreCase);
        }

        public async Task GoBackAsync()
        {
            var shell = Shell.Current;
            if(shell is null)
                throw new InvalidOperationException("Shell.Current is null. Ensure your app uses Shell.");

            // EHMR uses Dashboard as the single Back destination. Do not pop
            // the Shell stack: doing so can return to stale detail/create pages
            // and is the source of the inconsistent Back behavior across modules.
            var current = shell.CurrentState?.Location.OriginalString;
            var dashboard = $"//{AppRoutes.Dashboard}";

            if(string.Equals(current, dashboard, StringComparison.OrdinalIgnoreCase))
                return;

            await shell.GoToAsync(dashboard, false);
        }

        public async Task PushModalAsync(object page)
        {
            if(page is not Page p)
                throw new ArgumentException("Parameter must be of type Page", nameof(page));

            var currentPage = GetCurrentPage();
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