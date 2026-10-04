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
                    await activeShell.GoToAsync(normalizedRoute, true, navParams);
                else
                    await activeShell.GoToAsync(normalizedRoute, navParams);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed за рута [{route}]: {ex.Message}");
                throw;
            }

            _navEvents.NotifyRouteChanged(normalizedRoute);
            Debug.WriteLine($"Route called - on navService{DateTime.Now}", route);
        }

        public async Task GoBackAsync()
        {
            var shell = Shell.Current;
            if(shell is null)
                throw new InvalidOperationException("Shell.Current is null. Ensure your app uses Shell.");

            // Back must unwind the current Shell navigation stack first.
            // This keeps detail/create/edit pages on their originating list page
            // instead of forcing every Back action to the Dashboard.
            var navigationStack = shell.Navigation.NavigationStack;

            if(navigationStack.Count > 1)
            {
                await shell.Navigation.PopAsync(true);
                return;
            }

            // Root/menu pages have no previous page in the stack.
            // Only those fall back to the application Dashboard.
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