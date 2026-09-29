using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class MenuViewModel : ObservableObject, IDisposable
{
    private readonly IAuthStateService _auth;
    private readonly IMenuService _menuService;
    private readonly INavigationService _navigation;
    private readonly ISelectedItemService<Patient> _selectedPatientService;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private int _refreshToken;

    public ObservableCollection<NavigationGroup> Items { get; } = [];
    private readonly List<NavigationGroup> _allItems = [];
    private NavigationGroup? _focusedGroup;

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

    public MenuViewModel(
        IAuthStateService auth,
        IMenuService menuService,
        INavigationService navigation,
        ISelectedItemService<Patient> selectedPatientService)
    {
        _auth=auth;
        _menuService=menuService;
        _navigation=navigation;
        _selectedPatientService=selectedPatientService;
        _auth.AuthStateChanged+=OnAuthChanged;

        if(Shell.Current is not null)
            Shell.Current.Navigated+=OnShellNavigated;

        ApplyUserInfo();
        _=RefreshMenuAsync();
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        var route=ResolveMenuRoute(e.Current?.Location);
        if(string.IsNullOrWhiteSpace(route))
            return;

        ActiveRoute=route;

        if(e.Source==ShellNavigationSource.Pop||
           (_focusedGroup!=null&&!BelongsToGroup(_focusedGroup, route)))
            ShowMainMenu();
    }

    private string? ResolveMenuRoute(Uri? location)
    {
        if(location==null)
            return null;

        var route=location.OriginalString
            .Split(['/', '?', '#'], StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        if(string.IsNullOrWhiteSpace(route))
            return null;

        var exactRoute=_allItems
            .SelectMany(group => group.Items.Select(item => item.Route).Append(group.Route))
            .FirstOrDefault(itemRoute =>
                itemRoute.Equals(route, StringComparison.OrdinalIgnoreCase));

        if(!string.IsNullOrWhiteSpace(exactRoute))
            return exactRoute;

        var parentRoute=AppNavigation.ResolveMenuRoute(route);
        return _allItems
            .Select(group => group.Route)
            .FirstOrDefault(groupRoute =>
                groupRoute.Equals(parentRoute, StringComparison.OrdinalIgnoreCase));
    }

    private async void OnAuthChanged(object? sender, EventArgs e)
    {
        try
        {
            ApplyUserInfo();
            await RefreshMenuAsync();
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"OnAuthChanged failed: {ex}");
        }
    }

    [ObservableProperty]
    private string userName = "Гостин";

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private string userInitial = "?";

    private void ApplyUserInfo()
    {
        var user=_auth.CurrentUser;
        if(user==null)
        {
            UserName="Гостин";
            UserRole=string.Empty;
            UserInitial="?";
            return;
        }

        UserName=user.FirstName??user.Username??"Корисник";
        UserRole=user.Role.ToString()??"Нема привилегии";
        UserInitial=!string.IsNullOrWhiteSpace(UserName)
            ? UserName.Trim()[0].ToString().ToUpper()
            : "?";
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NavigateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ToggleGroupCommand))]
    private bool isNavigating;

    private bool CanNavigate() => !IsNavigating;

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task NavigateAsync(NavigationItem? item)
    {
        if(item==null||string.IsNullOrWhiteSpace(item.Route))
            return;

        if(ActiveRoute==item.Route&&!item.StartsNewRecord)
            return;

        if(!await _navigationLock.WaitAsync(0))
            return;

        var previousRoute=ActiveRoute;
        IsNavigating=true;

        if(item.StartsNewRecord&&item.Route==AppRoutes.Patients.Detail)
        {
            _selectedPatientService.SelectedItem=null;
            _selectedPatientService.OpenInEditMode=true;
        }

        try
        {
            ActiveRoute=item.Route;
            await _navigation.NavigateToRootAsync(item.Route);
        }
        catch(Exception ex)
        {
            ActiveRoute=previousRoute;
            System.Diagnostics.Debug.WriteLine($"Navigation to '{item.Route}' failed: {ex}");
        }
        finally
        {
            IsNavigating=false;
            _navigationLock.Release();
        }
    }

    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private async Task ToggleGroupAsync(NavigationGroup group)
    {
        if(group==null)
            return;

        if(group.Items.Count==0)
        {
            if(ActiveRoute==group.Route)
                return;

            if(!await _navigationLock.WaitAsync(0))
                return;

            var previousRoute=ActiveRoute;
            IsNavigating=true;

            try
            {
                if(!string.IsNullOrWhiteSpace(group.Route))
                {
                    ActiveRoute=group.Route;
                    await _navigation.NavigateToRootAsync(group.Route);
                }
            }
            catch(Exception ex)
            {
                ActiveRoute=previousRoute;
                System.Diagnostics.Debug.WriteLine($"Navigation to '{group.Route}' failed: {ex}");
            }
            finally
            {
                IsNavigating=false;
                _navigationLock.Release();
            }

            return;
        }

        if(ReferenceEquals(_focusedGroup, group)&&group.IsExpanded)
            ShowMainMenu();
        else
            FocusGroup(group);
    }

    private static string ToNavigationRoute(string route)
    {
        if(!await _navigationLock.WaitAsync(0))
            return;

        var previousRoute=ActiveRoute;
        IsNavigating=true;
        try
        {
            ActiveRoute=group.Route;
            await _navigation.NavigateToRootAsync(group.Route);
        }
        catch(Exception ex)
        {
            ActiveRoute=previousRoute;
            ShowMainMenu();
            System.Diagnostics.Debug.WriteLine(
                $"Navigation to '{group.Route}' failed: {ex}");
        }
        finally
        {
            IsNavigating=false;
            _navigationLock.Release();
        }
    }

    public async Task RefreshMenuAsync()
    {
        var token=++_refreshToken;

        try
        {
            var groups=await _menuService.UpdateMenuAsync();

            if(token!=_refreshToken)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if(token!=_refreshToken)
                    return;

                _allItems.Clear();
                foreach(var group in groups)
                    _allItems.Add(group);

                ShowMainMenu();

                ActiveRoute=ResolveMenuRoute(Shell.Current?.CurrentState?.Location)
                    ??ActiveRoute
                    ??AppRoutes.Dashboard;

                ApplyActiveState();
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menu refresh failed: {ex}");
        }
    }

    private string? _activeRoute;
    public string? ActiveRoute
    {
        get=>_activeRoute;
        set
        {
            if(SetProperty(ref _activeRoute,value))
                ApplyActiveState();
        }
    }

    private void ApplyActiveState()
    {
        foreach(var group in _allItems)
        {
            if(group.Items.Count==0)
            {
                group.IsActive=!string.IsNullOrWhiteSpace(group.Route)&&
                               group.Route.Equals(ActiveRoute,StringComparison.OrdinalIgnoreCase);
                group.IsExpanded=false;
                continue;
            }

            bool hasActiveChild=false;
            foreach(var item in group.Items)
            {
                item.IsActive=!string.IsNullOrWhiteSpace(item.Route)&&
                              item.Route.Equals(ActiveRoute,StringComparison.OrdinalIgnoreCase);
                if(item.IsActive)
                    hasActiveChild=true;
            }

            group.IsActive=hasActiveChild;
            group.IsExpanded=_focusedGroup!=null&&ReferenceEquals(group,_focusedGroup);
        }
    }

    private static bool BelongsToGroup(NavigationGroup group,string route)
    {
        var menuRoute=AppNavigation.ResolveMenuRoute(route);

        return group.Route.Equals(menuRoute,StringComparison.OrdinalIgnoreCase)||
               group.Items.Any(item=>item.Route.Equals(route,StringComparison.OrdinalIgnoreCase));
    }

    private void FocusGroup(NavigationGroup group)
    {
        _focusedGroup=group;

        foreach(var item in _allItems)
            item.IsExpanded=ReferenceEquals(item,group);

        if(Items.Count!=_allItems.Count||!Items.SequenceEqual(_allItems))
        {
            Items.Clear();
            foreach(var item in _allItems)
                Items.Add(item);
        }

        group.IsExpanded=true;
        group.IsActive=true;
    }

    private void ShowMainMenu()
    {
        _focusedGroup=null;
        Items.Clear();

        foreach(var group in _allItems)
        {
            group.IsExpanded=false;
            Items.Add(group);
        }

        ApplyActiveState();
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Clear();
        await _navigation.GoToAsync($"//{AppRoutes.Login}");
    }

    public void Dispose()
    {
        _auth.AuthStateChanged-=OnAuthChanged;

        if(Shell.Current is not null)
            Shell.Current.Navigated-=OnShellNavigated;

        _navigationLock.Dispose();
    }
}
