using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
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