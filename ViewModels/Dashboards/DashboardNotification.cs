using CommunityToolkit.Mvvm.ComponentModel;
using static EHMR.ViewModels.DashboardViewModel;

namespace EHMR.ViewModels;

public class DashboardNotification : ObservableObject
{
    public string Title { get; set; } = string.Empty;

    public string Time { get; set; } = string.Empty;

    public NotificationLevel Level
    {
        get; set;
    }
}