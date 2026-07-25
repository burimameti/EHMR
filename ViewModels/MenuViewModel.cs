using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class MenuViewModel : ObservableObject, IDisposable
{
    private readonly IAuthStateService _auth;
    private readonly IMenuService _menuService;
    private readonly INavigationService _navigation;

    // Ensures only one Shell navigation is ever in flight at a time.
    // Overlapping GoToAsync calls are the actual cause of the menu
    // "hanging" when tapped fast / repeatedly.
    private readonly SemaphoreSlim _navigationLock = new(1, 1);

    // Guards RefreshMenuAsync against out-of-order completion when
    // AuthStateChanged fires more than once in quick succession.
    private int _refreshToken;

    public ObservableCollection<NavigationGroup> Items { get; } = [];

    public MenuViewModel(
        IAuthStateService auth,
        IMenuService menuService,
        INavigationService navigation)
    {
        _auth=auth;
        _menuService=menuService;
        _navigation=navigation;
        _auth.AuthStateChanged+=OnAuthChanged;
        ApplyUserInfo();
        _=RefreshMenuAsync();
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

    // ============== USER CARD INFO ==============

    [ObservableProperty]
    private string userName = "Гостин";

    [ObservableProperty]
    private string userRole = string.Empty;

    [ObservableProperty]
    private string userInitial = "?";

    private void ApplyUserInfo()
    {
        var user = _auth.CurrentUser;
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

    // ============== NAVIGATION ==============

    // While true, the nav commands are disabled (see CanNavigate below),
    // so a second tap can't queue a second GoToAsync while one is pending.
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
        if(ActiveRoute==item.Route)
            return;

        if(!await _navigationLock.WaitAsync(0))
            return;

        var previousRoute = ActiveRoute;
        IsNavigating=true;

        try
        {
            ActiveRoute=item.Route;
            await _navigation.GoToAsync(item.Route);
        }
        catch(Exception ex)
        {
            // Roll back so the highlighted item matches what's actually
            // on screen if navigation failed.
            ActiveRoute=previousRoute;
            System.Diagnostics.Debug.WriteLine(
                $"Navigation to '{item.Route}' failed: {ex}");
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

            var previousRoute = ActiveRoute;
            IsNavigating=true;

            try
            {
                if(!string.IsNullOrWhiteSpace(group.Route))
                {
                    ActiveRoute=group.Route;
                    await _navigation.GoToAsync(group.Route);
                }
            }
            catch(Exception ex)
            {
                ActiveRoute=previousRoute;
                System.Diagnostics.Debug.WriteLine(
                    $"Navigation to '{group.Route}' failed: {ex}");
            }
            finally
            {
                IsNavigating=false;
                _navigationLock.Release();
            }
            return;
        }

        group.IsExpanded=!group.IsExpanded;
    }

    public async Task RefreshMenuAsync()
    {
        var token = ++_refreshToken;

        try
        {
            var groups = await _menuService.UpdateMenuAsync();

            // A newer refresh started and finished (or started) while we
            // were awaiting — drop this stale result instead of letting
            // it clobber newer data.
            if(token!=_refreshToken)
                return;

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if(token!=_refreshToken)
                    return;

                Items.Clear();
                foreach(var group in groups)
                    Items.Add(group);
                ActiveRoute??=AppRoutes.Dashboard;
                ApplyActiveState();
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Menu refresh failed: {ex}");
        }
    }

    private string? _activeRoute;
    public string? ActiveRoute
    {
        get => _activeRoute;
        set
        {
            if(SetProperty(ref _activeRoute, value))
            {
                ApplyActiveState();
            }
        }
    }

    private void ApplyActiveState()
    {
        foreach(var group in Items)
        {
            if(group.Items.Count==0)
            {
                group.IsActive=
                    !string.IsNullOrWhiteSpace(group.Route)&&
                    group.Route.Equals(
                        ActiveRoute,
                        StringComparison.OrdinalIgnoreCase);
                group.IsExpanded=false;
                continue;
            }
            bool hasActiveChild = false;
            foreach(var item in group.Items)
            {
                item.IsActive=
                    !string.IsNullOrWhiteSpace(item.Route)&&
                    item.Route.Equals(
                        ActiveRoute,
                        StringComparison.OrdinalIgnoreCase);
                if(item.IsActive)
                    hasActiveChild=true;
            }
            group.IsActive=hasActiveChild;
            group.IsExpanded=hasActiveChild;
        }
    }

    // ============== LOGOUT ==============

    [RelayCommand]
    private async Task LogoutAsync()
    {
        _auth.Clear();
        await _navigation.GoToAsync($"//{AppRoutes.Login}");
    }

    public void Dispose()
    {
        _auth.AuthStateChanged-=OnAuthChanged;
        _navigationLock.Dispose();
    }
}