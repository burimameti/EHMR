using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using EHMR.Extensions;
using System.Windows.Input;

namespace EHMR.ViewModels.Dashboard.Models;
public partial class DashboardNotification : ObservableObject
{
    [ObservableProperty] private string title = string.Empty;
    [ObservableProperty] private string time = string.Empty;
    [ObservableProperty] private AlertLevel level;
}

public sealed class DashboardAlertSummaryItem
{
    public required AlertLevel Level
    {
        get; init;
    }
    public required int Count
    {
        get; init;
    }
    public string Label { get; init; } = "";
    public string Icon { get; init; } = "";
    public Color AccentColor { get; init; } = Colors.Gray;
    public Color BackgroundColor { get; init; } = Colors.White;
    public ICommand? Command
    {
        get; init;
    }
}

public sealed class DashboardAlertItem
{
    public Guid Id
    {
        get; init;
    }
    public Guid PatientId
    {
        get; init;
    }
    public string PatientName { get; init; } = "";
    public string Message { get; init; } = "";
    public string CreatedAtText { get; init; } = "";
    public AlertLevel Level
    {
        get; init;
    }
    public string LevelLabel => Level.ToLabel();
    public string Icon => Level.ToIcon();
    public Color AccentColor => Level.ToAccentColor();
    public Color BackgroundColor => Level.ToBackgroundColor();
    public ICommand? Command
    {
        get; init;
    }
}
