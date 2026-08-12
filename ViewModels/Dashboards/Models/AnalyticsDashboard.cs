using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Domain.Entities;
using System.Windows.Input;

namespace EHMR.ViewModels.Dashboard.Models;
public partial class DashboardEncounterItem : ObservableObject
{
    public Encounter Source { get; set; } = null!;
    [ObservableProperty] private string patientName = string.Empty;
    [ObservableProperty] private string nationalId = string.Empty;
    [ObservableProperty] private string szboNumber = string.Empty;
    [ObservableProperty] private string time = string.Empty;
    [ObservableProperty] private string statusText = string.Empty;
    [ObservableProperty] private Color statusColor = Colors.Transparent;
}
public partial class DashboardDayItem : ObservableObject
{
    [ObservableProperty] private DateTime date;
    [ObservableProperty] private string dayLabel = string.Empty;
    [ObservableProperty] private string dayNumber = string.Empty;
    [ObservableProperty] private bool isSelected;
    [ObservableProperty] private int encounterCount;
    public ICommand? Command
    {
        get; set;
    }

    public bool HasEncounters => EncounterCount>0;

    partial void OnEncounterCountChanged(int value) => OnPropertyChanged(nameof(HasEncounters));
}
