using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.ApplicationModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Constants;

namespace EHMR.ViewModels;

public partial class MenuViewModel : ObservableObject, IDisposable
{
    private readonly IAuthStateService _auth;
    private readonly IMenuService _menuService;
    private readonly INavigationService _navigation;

    public ObservableCollection<NavigationGroup> Items { get; } = new();

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

    public MenuViewModel(
        IAuthStateService auth,
        IMenuService menuService,
        INavigationService navigation)
    {
        _auth=auth;
        _menuService=menuService;
        _navigation=navigation;

        _auth.AuthStateChanged+=OnAuthChanged;

        // Почетно вчитување
        RefreshMenu();
    }

    private void OnAuthChanged(object? sender, EventArgs e)
        => RefreshMenu();

    [RelayCommand]
    private async Task NavigateAsync(NavigationItem? item)
    {
        if(item is null||string.IsNullOrWhiteSpace(item.Route))
            return;

        if(ActiveRoute==item.Route)
            return;

        ActiveRoute=item.Route;

        // Со користење на "//" му кажуваш на Shell дека ова е топ-левел дестинација
        // Ако рутата ти е регистрирана со Routing.RegisterRoute, пробај прво вака:
        await _navigation.GoToAsync(item.Route);
    }

    [RelayCommand]
    public async Task ToggleGroup(NavigationGroup group)
    {
        // Ако нема деца (Како Главен Прозор), веднаш правиме директна навигација
        if(group.Items==null||group.Items.Count==0)
        {
            ActiveRoute=group.Route; // Постави ја активната рута за да светне иконата
            await Shell.Current.GoToAsync($"//{group.Route}");
            return;
        }

        // Ако има подменија, класично отвори/затвори го менито
        group.IsExpanded=!group.IsExpanded;
    }

    public async void RefreshMenu()
    {
        try
        {
            var groups = await _menuService.UpdateMenuAsync(_auth.Permissions, _auth.Roles);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Items.Clear();
                foreach(var group in groups)
                {
                    Items.Add(group);
                }

                // Стандардно селектирај го дашбордот ако нема активна рута во моментот
                if(string.IsNullOrEmpty(ActiveRoute))
                {
                    ActiveRoute="dashboard";
                }
                else
                {
                    ApplyActiveState();
                }
            });
        }
        catch(Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Грешка при мени: {ex.Message}");
        }
    }

    private void ApplyActiveState()
    {
        foreach(var group in Items)
        {
            // ПРВО: Проверка дали самата група е директен линк (Како Главен Прозор)
            if(group.Items==null||group.Items.Count==0)
            {
                group.IsActive=!string.IsNullOrWhiteSpace(group.Route)&&group.Route==ActiveRoute;
                group.IsExpanded=false;
                continue;
            }

            // ВТОРО: Проверка за групи што содржат подменија (деца)
            bool hasActiveChild = false;
            foreach(var item in group.Items)
            {
                item.IsActive=!string.IsNullOrWhiteSpace(item.Route)&&item.Route==ActiveRoute;

                if(item.IsActive)
                    hasActiveChild=true;
            }

            // Групата е активна ако некое нејзино дете е селектирано
            group.IsActive=hasActiveChild;
            group.IsExpanded=hasActiveChild;
        }
    }

    public void Dispose()
    {
        _auth.AuthStateChanged-=OnAuthChanged;
    }
}