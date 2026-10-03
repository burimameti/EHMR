// File: EHMR.Backups/ViewModels/BackupDashboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Color = Microsoft.Maui.Graphics.Color;

namespace EHMR.Backups.ViewModels;

public partial class BackupDashboardViewModel : ObservableObject
{
    private readonly IBackupHistoryRepository _historyRepository;
    private readonly IDatabaseProviderResolver _databaseResolver;
    private readonly IStorageProviderResolver _storageResolver;
    private readonly IEnumerable<IBackupStorageProvider> _storageProviders;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty]
    private string lastBackup = "Never";

    [ObservableProperty]
    private string databaseSize = "0 MB";

    [ObservableProperty]
    private string backupStatus = "Unknown";

    // Change the type of statusColor from System.Drawing.Color to Microsoft.Maui.Graphics.Color
    [ObservableProperty]
    private Color statusColor = Colors.Gray;

    [ObservableProperty]
    private int destinationCount;

    [ObservableProperty]
    private bool isBusy;

    // Grid-bound properties (populated from recent history)
    [ObservableProperty]
    private List<string> gridColumns = new() { "Датум", "База", "Големина", "Статус", "Дестинација" };

    [ObservableProperty]
    private List<IList<string>> gridRows = new();

    [ObservableProperty]
    private int currentPage = 1;

    [ObservableProperty]
    private int totalPages = 1;

    public BackupDashboardViewModel(
        IBackupHistoryRepository historyRepository,
        IDatabaseProviderResolver databaseResolver,
        IStorageProviderResolver storageResolver,
        IEnumerable<IBackupStorageProvider> storageProviders)
    {
        _historyRepository=historyRepository;
        _databaseResolver=databaseResolver;
        _storageResolver=storageResolver;
        _storageProviders=storageProviders;
        _authorization=authorization;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;

            var last = await _historyRepository.GetLastAsync();

            LastBackup=last?.CompletedAt is { } completed
                ? completed.ToLocalTime().ToString("dd.MM.yyyy")
                : "Never";

            BackupStatus=last?.Status switch
            {
                Models.BackupRunStatus.Succeeded => "Успешно",
                Models.BackupRunStatus.Failed => "Неуспешно",
                Models.BackupRunStatus.Running => "Во тек",
                _ => "Непознато"
            };

            StatusColor=last?.Status switch
            {
                Models.BackupRunStatus.Succeeded => Colors.Green,
                Models.BackupRunStatus.Failed => Colors.Red,
                Models.BackupRunStatus.Running => Colors.Orange,
                _ => Colors.Gray
            };

            var provider = _databaseResolver.Resolve();
            var sizeBytes = await provider.GetDatabaseSizeAsync();
            DatabaseSize=FormatSize(sizeBytes);

            DestinationCount=_storageProviders.Count();

            var recent = await _historyRepository.GetRecentAsync(10);
            GridRows=recent
                .Select(h => (IList<string>)new List<string>
                {
                    h.CompletedAt?.ToLocalTime().ToString("g") ?? h.StartedAt.ToLocalTime().ToString("g"),
                    h.DatabaseName,
                    FormatSize(h.Size),
                    h.Status.ToString(),
                    h.DestinationName
                })
                .ToList();
        }
        finally
        {
            IsBusy=false;
        }
    }

    /// <summary>
    /// Отвора управување со дестинациите. Групата за резервни копии во AppNavigation
    /// е закоментирана, па страницата нема ставка во менито — до неа се стигнува оттука.
    /// </summary>
    [RelayCommand]
    private async Task OpenDestinationsAsync()
        => await Shell.Current.GoToAsync(AppRoutes.Backup.Destinations);

    [RelayCommand]
    private async Task RunBackupAsync()
    {
        // Navigation to BackupPage happens at the View layer (Shell.Current.GoToAsync),
        // this command is just the trigger the header button binds to.
        await Shell.Current.GoToAsync("BackupPage");
    }

    private static string FormatSize(long bytes)
    {
        return bytes switch
        {
            <=0 => "0 MB",
            <1024L*1024*1024 => $"{bytes/1024.0/1024.0:F1} MB",
            _ => $"{bytes/1024.0/1024.0/1024.0:F2} GB"
        };
    }
}