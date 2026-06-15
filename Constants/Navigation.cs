using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace EHMR.Constants;

public partial class NavigationGroup : ObservableObject
{
    public string GroupTitle { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public IconDefinition Icon { get; set; } = new IconDefinition();
    public string Module { get; set; } = string.Empty;

    public List<string> RequiredPermissions { get; set; } = new();

    // Листа на потставки во оваа група
    public List<NavigationItem> Items { get; set; } = new();

    // Својство кое контролира дали групата е визуелно отворена во MAUI акцелероторот/менито
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isActive;
}

public partial class NavigationItem : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public IconDefinition Icon { get; set; } = new IconDefinition();
    public string Module { get; set; } = string.Empty;

    public List<string> RequiredPermissions { get; set; } = new();

    // Ова својство го користиме во MenuViewModel.ApplyActiveState()
    // за визуелно да го обоиме активното мени во сино/сиво (селектиран маркер)
    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}