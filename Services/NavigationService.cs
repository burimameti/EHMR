using EHMR.Domain.Interfaces;
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

                // 2. Изврши ја навигацијата преку пронајдениот Shell
                if(route.StartsWith("//"))
                {
                    await activeShell.GoToAsync(route, true, navParams);
                }
                else
                {
                    // Поправка за релативни рути:
                    // За да биде посигурно со детални страници регистрирани во C#, секогаш е подобро со чисто име:
                    await activeShell.GoToAsync(route, navParams);
                }
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation failed за рута [{route}]: {ex.Message}");
                throw;
            }

            _navEvents.NotifyRouteChanged(route);
            Debug.WriteLine($"Route called - on navService{DateTime.Now}", route);
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