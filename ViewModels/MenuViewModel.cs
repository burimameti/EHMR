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

        _=RefreshMenuAsync();
    }

    private async void OnAuthChanged(object? sender, EventArgs e)
    {
        await RefreshMenuAsync();
    }

    [RelayCommand]
    private async Task NavigateAsync(NavigationItem? item)
    {
        if(item==null||string.IsNullOrWhiteSpace(item.Route))
            return;

        if(ActiveRoute==item.Route)
            return;

        ActiveRoute=item.Route;

        await _navigation.GoToAsync(item.Route);
    }

    [RelayCommand]
    private async Task ToggleGroupAsync(NavigationGroup group)
    {
        if(group==null)
            return;

        // директен линк
        if(group.Items.Count==0)
        {
            ActiveRoute=group.Route;

            if(!string.IsNullOrWhiteSpace(group.Route))
                await _navigation.GoToAsync(group.Route);

            return;
        }

        group.IsExpanded=!group.IsExpanded;
    }

    public async Task RefreshMenuAsync()
    {
        try
        {
            var groups = await _menuService.UpdateMenuAsync();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
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
            // group without children
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

    public void Dispose()
    {
        _auth.AuthStateChanged-=OnAuthChanged;
    }
}