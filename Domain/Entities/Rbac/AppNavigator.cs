using EHMR.Domain.Entities.Rbac;
using System.Diagnostics;

namespace EHMR.Services;

/// <summary>
/// Единствено место за Shell навигација.
///
/// Проблем што го решава: AppRoutes рутите се обични имиња ("patientslist"), па
/// Shell.GoToAsync("patientslist") прави релативен PUSH. Секое кликање во менито
/// додаваше нова страница врз стекот (со свое мени, subscriptions и барања до база).
///
/// Правила:
///  • "//..", ".." и "../.." се пропуштаат како што се.
///  • dashboard / login          → "//dashboard", "//login" (замена на root).
///  • листи и менија (RootRoutes) → "//dashboard/{рута}": стекот се празни и
///    страницата се отвора врз dashboard, па повеќе не се натрупува.
///  • детали / create / edit      → релативен push, како и досега (враќањето работи со "..").
///  • Само една навигација истовремено; двојно кликање се игнорира.
/// </summary>
public static class AppNavigator
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>
    /// Патеки од највисоко ниво: ставки од менито и листи. Додади овде секоја нова
    /// листа/мени-страница. Детали (…detail, create, edit) НЕ одат тука.
    /// </summary>
    private static readonly HashSet<string> RootRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        AppRoutes.Calendar,
        AppRoutes.CalendarPage,
        AppRoutes.Admin.AdminPanel,

        AppRoutes.Patients.List,
        AppRoutes.Doctors.List,
        AppRoutes.Encounters.List,
        AppRoutes.Appointments.List,
        AppRoutes.Medicines.List,
        AppRoutes.Prescriptions.List,
        AppRoutes.Protocols.List,
        AppRoutes.Therapy.List,
        AppRoutes.Users.List,
        AppRoutes.Reports.List,
        AppRoutes.Mkb10Codes.List,

        AppRoutes.Backup.Dashboard,
        AppRoutes.Backup.Backups,
        AppRoutes.Backup.Restore,
        AppRoutes.Backup.History,
        AppRoutes.Backup.Destinations,
    };

    public static string Resolve(string route, bool forceRoot = false)
    {
        route=route.Trim();

        if(route.StartsWith("//")||route.StartsWith(".."))
            return route;

        var path = route;
        var query = string.Empty;
        var q = route.IndexOf('?');
        if(q>=0)
        {
            path=route[..q];
            query=route[q..];
        }

        if(path.Equals(AppRoutes.Dashboard, StringComparison.OrdinalIgnoreCase)||
           path.Equals(AppRoutes.Login, StringComparison.OrdinalIgnoreCase))
            return $"//{path}{query}";

        if(forceRoot||RootRoutes.Contains(path))
            return $"//{AppRoutes.Dashboard}/{path}{query}";

        return route;
    }

    public static async Task GoAsync(
        Shell shell,
        string route,
        IDictionary<string, object>? parameters = null,
        bool forceRoot = false)
    {
        var target = Resolve(route, forceRoot);

        // Ако веќе тече навигација, ова кликање се игнорира (нема втор GoToAsync).
        if(!await Gate.WaitAsync(0))
        {
            Debug.WriteLine($"[Nav] '{target}' игнорирана — друга навигација е во тек.");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if(parameters is { Count:>0 })
                    await shell.GoToAsync(target, parameters);
                else
                    await shell.GoToAsync(target);
            });
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>
    /// За директните повици Shell.Current.GoToRouteAsync(...) низ ViewModel-ите —
    /// иста логика и заклучување како NavigationService.
    /// </summary>
    public static Task GoToRouteAsync(
        this Shell shell,
        string route,
        IDictionary<string, object>? parameters = null)
        => GoAsync(shell, route, parameters);
}