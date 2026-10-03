using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using System.Diagnostics;

namespace EHMR.Services;

public class NavigationService : INavigationService
{
    private readonly INavigationEvents _navEvents;

    public NavigationService(INavigationEvents navEvents)
    {
        _navEvents = navEvents;
    }

    public async Task NavigateToAsync<TViewModel>(IDictionary<string, object>? parameters = null)
    {
        var route = typeof(TViewModel).Name.Replace("ViewModel", "Page");
        await GoToAsync(route, parameters);
    }

    public async Task GoToAsync(
        string route,
        IDictionary<string, object>? parameters = null)
    {
        if(string.IsNullOrWhiteSpace(route))
            throw new ArgumentNullException(nameof(route));

        var shell = Shell.Current ?? Application.Current?.MainPage as Shell;
        if(shell is null)
            throw new InvalidOperationException(
                "Shell.Current is null. Ensure the application Shell is initialized.");

        var normalized = route.Trim('/');
        var shellRoute = shell.Items.Any(item =>
            item.Route.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        // ShellItem/ShellContent routes cannot be navigated as relative routes.
        // Registered child pages remain relative routes.
        var navigationRoute = shellRoute
            ? $"///{normalized}"
            : normalized;

        try
        {
            await shell.GoToAsync(
                navigationRoute,
                false,
                parameters ?? new Dictionary<string, object>());

            _navEvents.NotifyRouteChanged(normalized);
            Debug.WriteLine($"Navigation completed: {normalized}");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"Navigation failed for [{navigationRoute}]: {ex}");
            throw;
        }
    }

    public async Task GoBackAsync()
    {
        var shell = Shell.Current;
        if(shell is null)
            throw new InvalidOperationException(
                "Shell.Current is null. Ensure the application Shell is initialized.");

        await shell.GoToAsync($"//{AppRoutes.Dashboard}", true);
        _navEvents.NotifyRouteChanged(AppRoutes.Dashboard);
    }

    public async Task PushModalAsync(object page)
    {
        if(page is not Page p)
            throw new ArgumentException("Parameter must be of type Page", nameof(page));

        var currentPage = Application.Current?.Windows?.FirstOrDefault()?.Page;
        if(currentPage is null)
            throw new InvalidOperationException("No active application page.");

        await currentPage.Navigation.PushModalAsync(p);
    }

    public async Task<object?> PopAsync()
    {
        if(Shell.Current is null)
            throw new InvalidOperationException(
                "Shell.Current is null. Ensure the application Shell is initialized.");

        return await Shell.Current.Navigation.PopAsync();
    }
}
