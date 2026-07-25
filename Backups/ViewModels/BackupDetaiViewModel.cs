
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls.Charts;
using System.Collections.ObjectModel;

namespace EHMR.Backups.ViewModels;

/// <summary>
/// Backs BackupDetailsPage — shows the selected backup's own info plus two charts built
/// from the last 30 runs: a size trend bar chart and a success/failed donut.
/// Assumes ISelectedItemService&lt;BackupHistory&gt;.SelectedItem was set by whoever
/// navigated here (see BackupHistoryViewModel.OpenDetailsAsync).
/// </summary>
public partial class BackupDetailsViewModel : ObservableObject
{
    private readonly IBackupHistoryRepository _historyRepository;
    private readonly ISelectedItemService<BackupHistory> _selectedItemService;

    [ObservableProperty]
    private BackupHistory? backup;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private ObservableCollection<ChartDataPoint> sizeTrend = new();

    [ObservableProperty]
    private ObservableCollection<DonutSegment> statusBreakdown = new();

    [ObservableProperty]
    private string statusBreakdownCenterText = string.Empty;

    [ObservableProperty]
    private int totalBackups;

    [ObservableProperty]
    private int successfulBackups;

    [ObservableProperty]
    private int failedBackups;

    [ObservableProperty]
    private double successRatePercentage;

    public BackupDetailsViewModel(
        IBackupHistoryRepository historyRepository,
        ISelectedItemService<BackupHistory> selectedItemService)
    {
        _historyRepository=historyRepository;
        _selectedItemService=selectedItemService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy)
            return;

        try
        {
            IsBusy=true;

            Backup=_selectedItemService.SelectedItem;

            var recent = (await _historyRepository.GetRecentAsync(30)).ToList();

            TotalBackups=recent.Count;
            SuccessfulBackups=recent.Count(x => x.Status==BackupRunStatus.Succeeded);
            FailedBackups=TotalBackups-SuccessfulBackups;
            SuccessRatePercentage=TotalBackups>0
                ? Math.Round(SuccessfulBackups*100.0/TotalBackups, 1)
                : 0;

            SizeTrend=new ObservableCollection<ChartDataPoint>(
                recent
                    .OrderBy(x => x.StartedAt)
                    .TakeLast(10)
                    .Select(x => new ChartDataPoint
                    {
                        Label=x.StartedAt.ToString("dd.MM"),
                        Value=Math.Round(x.Size/1024.0/1024.0, 1) // bytes -> MB
                    }));

            StatusBreakdown=new ObservableCollection<DonutSegment>
            {
                new() { Label = "Успешни", Value = SuccessfulBackups, Color = Color.FromArgb("#21B6C4") },
                new() { Label = "Неуспешни", Value = FailedBackups, Color = Color.FromArgb("#E0554F") }
            };

            StatusBreakdownCenterText=$"{SuccessRatePercentage:0.#}%";
        }
        finally
        {
            IsBusy=false;
        }
    }
}
