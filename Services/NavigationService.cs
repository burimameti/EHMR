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
                 normalizedRoute=route switch
                {
                    "dashboard" => "//dashboard",
                    "login" => "//login",
                    "patients" => "//patients",
                    _ => route
                };

                if(normalizedRoute.StartsWith("//"))
                {
                    await activeShell.GoToAsync(normalizedRoute, true, navParams);
                }
                else
                {
                    // Module/list routes are navigation roots. Do not leave an old
                    // Calendar/List page underneath them; otherwise Back can reopen
                    // a stale page from an earlier navigation flow.
                    if(IsModuleRootRoute(normalizedRoute) &&
                       activeShell.Navigation.NavigationStack.Count > 1)
                    {
                        await activeShell.Navigation.PopToRootAsync(false);
                    }

                    await activeShell.GoToAsync(normalizedRoute, navParams);
                }
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

            // Prefer the real Shell navigation stack. This preserves the user's
            // flow (list -> detail -> edit -> back) instead of always jumping to Dashboard.
            // Fall back to Dashboard only when there is no previous page in the stack.
            var navigation = shell.Navigation;
            if(navigation.NavigationStack.Count>1)
            {
                await navigation.PopAsync();
                return;
            }

            if(!string.Equals(shell.CurrentState?.Location.OriginalString, $"//{AppRoutes.Dashboard}", StringComparison.OrdinalIgnoreCase))
                await shell.GoToAsync($"//{AppRoutes.Dashboard}", true);
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