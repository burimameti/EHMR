using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities.Rbac;

namespace EHMR.Constants;

public partial class NavigationGroup : ObservableObject
{
    public string GroupTitle { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public IconDefinition Icon { get; set; } = new IconDefinition();
    public string Module { get; set; } = string.Empty;

    // Листа на потставки во оваа група
    public List<NavigationItem> Items { get; set; } = new();

    // Својство кое контролира дали групата е визуелно отворена во MAUI акцелероторот/менито
    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    [ObservableProperty]
    private bool _isHovered;

    private bool _isActive;
}

public partial class NavigationItem : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public IconDefinition Icon { get; set; } = new IconDefinition();
    public string Module { get; set; } = string.Empty;
    public ModuleAction RequiredAction { get; set; } = ModuleAction.View;
    public bool StartsNewRecord { get; set; }

    private bool _isActive;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}