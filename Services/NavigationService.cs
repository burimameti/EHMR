using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities.Rbac;
using System.Diagnostics;

namespace EHMR.Services
{
    public class NavigationService : INavigationService
    {
        // All navigation initiated through the service is serialized. Shell is not
        // safe to transition concurrently from menu, Back, and form commands.
        private static readonly SemaphoreSlim NavigationLock = new(1, 1);
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
            await NavigationLock.WaitAsync();
            try
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

                // Routes declared as Shell hierarchy items (FlyoutItem/ShellItem)
                // must use an absolute Shell URI. Pages registered with
                // Routing.RegisterRoute() must stay relative. Detect the former
                // from the actual Shell instead of maintaining a fragile hard-coded
                // list of root routes.
                var cleanRoute = route.Split('?', 2)[0].Trim('/');
                var shellRoot = activeShell.Items.FirstOrDefault(item =>
                    string.Equals(item.Route, cleanRoute, StringComparison.OrdinalIgnoreCase));

                normalizedRoute = shellRoot is not null
                    ? $"//{cleanRoute}"
                    : route.TrimStart('/');

                // Menu navigation should be a single Shell operation. The previous
                // implementation performed PopToRootAsync + GoToAsync for module
                // routes, which made every menu click wait for two navigation
                // transitions. Disable Shell animation as well; page loading remains
                // owned by the destination ViewModel.
                await activeShell.GoToAsync(
                    normalizedRoute,
                    false,
                    navParams);

                // Registered Shell routes can leave their previous pages on the
                // navigation stack. Keep the Shell root and the destination only,
                // disposing transient page view-models before removing stale pages.
                DisposeStaleRoutePages(activeShell);
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed за рута [{route}]: {ex.Message}");
                throw;
            }

            _navEvents.NotifyRouteChanged(normalizedRoute);
            Debug.WriteLine($"Route called - on navService{DateTime.Now}", route);
            }
            finally
            {
                NavigationLock.Release();
            }
        }

        private static void DisposeStaleRoutePages(Shell shell)
        {
            var stack = shell.Navigation.NavigationStack;
            if(stack.Count <= 2)
                return;

            // Preserve the root page and the newly navigated-to page.
            for(var index = stack.Count - 2; index >= 1; index--)
            {
                var stalePage = stack[index];

                if(stalePage.BindingContext is IDisposable disposable &&
                   stalePage.BindingContext is not ViewModels.MenuViewModel)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine($"Failed to dispose view-model for stale route '{stalePage.GetType().Name}': {ex}");
                    }
                }

                try
                {
                    shell.Navigation.RemovePage(stalePage);
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Failed to remove stale route page '{stalePage.GetType().Name}': {ex}");
                }
            }
        }

        public async Task GoBackAsync()
        {
            await NavigationLock.WaitAsync();
            try
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
            finally
            {
                NavigationLock.Release();
            }
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