// File: EHMR.Backups/ViewModels/BackupHistoryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using EHMR.ViewModels;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace EHMR.Backups.ViewModels;

public partial class BackupHistoryViewModel : BaseViewModel<BackupHistory>
{
    private readonly IBackupHistoryRepository _historyRepository;
    [ObservableProperty]
    private SparkGridRow selectedBackupRow;

    [ObservableProperty]
    private ObservableCollection<SparkGridColumn> gridColumns = new();

    [ObservableProperty]
    private ObservableCollection<SparkGridRow> gridRows = new();

    private readonly Dictionary<string, SparkTabItem> _statusTabs = new();
    public ObservableCollection<string> Destinations { get; } = new();
    [ObservableProperty]
    private string selectedDestination = "All";

    private SparkPickerItem _statusPicker;
    private SparkPickerItem _destinationPicker;
    [ObservableProperty]
    private int totalBackups;

    [ObservableProperty]
    private int successfulBackups;

    [ObservableProperty]
    private int failedBackups;
    private bool _sparkInitialized;
    [ObservableProperty]
    private string totalBackupSize = "";
    protected override string ModuleName => Modules.Backups;

    public BackupHistoryViewModel(
        IBackupHistoryRepository historyRepository,
        INavigationService navigationService,
        IUserDialogService dialogService,
        IMenuService menuService,
        IAuthorizationService authorizationService,
        ISelectedItemService<BackupHistory> selectedItemService)
        : base(
            navigationService,
            dialogService,
            menuService,
            authorizationService,
            selectedItemService)
    {
        _historyRepository=historyRepository;

        PageSize=20;

        EvaluatePermissions();
    }
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy)
            return;

        try
        {
            IsBusy=true;

            ClearError();

            var data = await _historyRepository.GetRecentAsync(500);

            AllItems=data.ToList();

            RefreshDestinationLookup();

           // if(!_sparkInitialized)
            {
                InitializeSparkControls();
                _sparkInitialized=true;
            }

            RefreshSparkTabCounts();

            ApplyPipeline();

            RecomputeMetrics();
            RefreshPage();

            // StatusMessage=$"{TotalItems} backups loaded.";
        }
        catch(Exception ex)
        {
            OnError(ex.Message);
        }
        finally
        {
            IsBusy=false;
        }
    }
    private void RefreshDestinationLookup()
    {
        Destinations.Clear();

        Destinations.Add("All");

        foreach(var destination in AllItems
            .Select(x => x.DestinationName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .OrderBy(x => x))
        {
            Destinations.Add(destination);
        }

        if(!Destinations.Contains(SelectedDestination))
            SelectedDestination="All";
    }
    private void RecomputeMetrics()
    {
        TotalBackups=AllItems.Count;

        SuccessfulBackups=
      AllItems.Count(x => x.Status==BackupRunStatus.Succeeded);

        FailedBackups=
            AllItems.Count(x =>
                x.Status==BackupRunStatus.Failed);

        FailedBackups=TotalBackups-SuccessfulBackups;

        TotalBackupSize=
            FormatBytes(AllItems.Sum(x => x.Size));
    }
    protected override IEnumerable<BackupHistory> ApplySearch(
    IEnumerable<BackupHistory> query,
    string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return query;

        search=search.Trim();

        return query.Where(x =>
            (x.DatabaseName?.Contains(search, StringComparison.OrdinalIgnoreCase)??false)||
            (x.DestinationName?.Contains(search, StringComparison.OrdinalIgnoreCase)??false));
    }
    protected override IEnumerable<BackupHistory> ApplyFilters(IEnumerable<BackupHistory> query)
    {
        if(SelectedStatus!="All")
        {
            query=query.Where(x =>
                x.Status.ToString().Equals(
                    SelectedStatus,
                    StringComparison.OrdinalIgnoreCase));
        }

        if(SelectedDestination!="All")
            query=query.Where(x =>
                x.DestinationName==SelectedDestination);

        return query;
    }
    protected override IEnumerable<BackupHistory> ApplySort(
    IEnumerable<BackupHistory> query)
    {
        return query
            .OrderByDescending(x => x.StartedAt);
    }
    protected override void OnPageProjected(ObservableCollection<BackupHistory> page)
    {
        System.Diagnostics.Debug.WriteLine($"OnPageProjected: {page.Count} rows");
        RefreshSparkGridRows(page);
    }
    protected override void ResetFilters()
    {
        SearchText=string.Empty;
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if(SelectedBackupRow?.Tag is not BackupHistory backup)
        {
            await UserDialogService.ShowAlertAsync(
                "Backup",
                "Изберете бекап.");

            return;
        }

        await _historyRepository.DeleteAsync(backup.Id);

        await LoadAsync();
    }

    [RelayCommand]
    private async Task RunBackupAsync()
    {
        // if(SelectedBackup==null) return;
        await Shell.Current.GoToAsync("BackupPage");
    }

    [ObservableProperty]
    private string selectedStatus = "All";

    public ObservableCollection<string> StatusFilters
    {
        get;
    } =
    [
        "Сите",
    "Успешни",
    "Неуспешни",
    "Се извршува",
    "Откажани"
    ];


    private async Task DeleteBackupAsync(
    BackupHistory backup)
    {
        await _historyRepository.DeleteAsync(backup.Id);

        await LoadAsync();
    }
    private async Task RestoreAsync(
    BackupHistory backup)
    {
        await UserDialogService.ShowAlertAsync(
            "Restore",
            "Restore will be implemented soon.");
    }
    private async Task OpenDetailsAsync(
    BackupHistory backup)
    {
        SelectedItemService.SelectedItem=backup;

        await NavigationService.GoToAsync(
            AppRoutes.Backup.BackupDetails);
    }
    partial void OnSelectedBackupRowChanged(
      SparkGridRow value)
    {
        if(value?.Tag is BackupHistory backup)
        {
            SelectedItemService.SelectedItem=backup;
        }
    }
    partial void OnSelectedDestinationChanged(string value)
    {
        ApplyPipeline();
    }
    protected override void SyncSparkPickersFromFilters()
    {
        _statusPicker.SelectedItem=SelectedStatus;
        _destinationPicker.SelectedItem=SelectedDestination;
    }
    private void BuildSparkPickers()
    {
        Pickers.Clear();

        _statusPicker=MakePicker(
            "Статус",
            StatusFilters,
            SelectedStatus,
            x => SelectedStatus=x);

        //_destinationPicker=MakePicker(
        //    "Destination",
        //    Destinations,
        //    SelectedDestination,
        //    x => SelectedDestination=x);

        Pickers.Add(_statusPicker);
        //Pickers.Add(_destinationPicker);
    }
    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabs.Clear();

        foreach(var status in StatusFilters)
        {
            var tab = new SparkTabItem
            {
                Title=status,
                IsSelected=status==SelectedStatus
            };

            tab.Command=new RelayCommand(() =>
            {
                SelectTab(tab, () => SelectedStatus=status);
            });

            Tabs.Add(tab);

            _statusTabs[status]=tab;
        }

        RefreshSparkTabCounts();

      //  SyncSparkPickersFromFilters();
    }
    partial void OnSelectedStatusChanged(string value)
    {
        foreach(var pair in _statusTabs)
            pair.Value.IsSelected=pair.Key==value;

        SyncSparkPickersFromFilters();

        ApplyPipeline();
    }
    private void RefreshSparkTabCounts()
    {
        foreach(var pair in _statusTabs)
        {
            pair.Value.Value=
     pair.Key=="All"
         ? AllItems.Count.ToString()
         : AllItems.Count(x => x.Status.ToString()==pair.Key).ToString();
        }
    }
    protected override void BuildSparkButtons()
    {
        Buttons.Clear();

        Buttons.Add(new SparkButtonItem
        {
            Label="Нов Бекап",
            IconGlyph="\uf0c7",
            IsPrimary=true,
            Command=RunBackupCommand
        });

        Buttons.Add(new SparkButtonItem
        {
            Label="Освежи",
            IconGlyph="\uf021",
            Command=LoadCommand
        });

        Buttons.Add(new SparkButtonItem
        {
            Label="Избриши",
            IconGlyph="\uf1f8",
            Command=DeleteCommand
        });


    }
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }
    private void BuildSparkGridColumns()
    {
        GridColumns=
        [
            new()
        {
            Header="ДАТУМ",
            Key="Date",
            Width=new GridLength(1.6,GridUnitType.Star)
        },

        new()
        {
            Header="БАЗА",
            Key="Database",
            Width=new GridLength(2.2,GridUnitType.Star)
        },

        new()
        {
            Header="ДЕСТИНАЦИЈА",
            Key="Destination",
            Width=new GridLength(1.8,GridUnitType.Star)
        },

        new()
        {
            Header="ГОЛЕМИНА",
            Key="Size",
            Width=new GridLength(1.0,GridUnitType.Star)
        },

        new()
        {
            Header="СТАТУС",
            Key="Status",
            CellType=SparkGridCellType.Badge,
            Width=new GridLength(1.0,GridUnitType.Star)
        },

        new()
        {
            Header="АКЦИИ",
            Key="Actions",
            CellType=SparkGridCellType.Actions,
            Width=GridLength.Auto
        }
        ];
    }


    private static string FormatBytes(long bytes)
    {
        string[] units =
        {
        "B",
        "KB",
        "MB",
        "GB",
        "TB"
    };

        double size = bytes;

        int order = 0;

        while(size>=1024&&order<units.Length-1)
        {
            order++;
            size/=1024;
        }

        return $"{size:0.##} {units[order]}";
    }

    private void RefreshSparkGridRows(ObservableCollection<BackupHistory> backups)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var backup in backups)
        {
            var row = new SparkGridRow { Tag=backup };
            row["Date"]=backup.StartedAt.ToString("dd.MM.yyyy HH:mm");
            row["Database"]=backup.DatabaseName;
            row["Destination"]=backup.DestinationName;
            row["Size"]=FormatBytes(backup.Size);
            row["Status"]=new SparkBadgeValue(
        backup.Status switch
        {
            BackupRunStatus.Succeeded => "Успешно",
            BackupRunStatus.Failed => "Неуспешно",
            BackupRunStatus.Running => "Се Извршува",
            BackupRunStatus.Verifying => "Проверува",
            _ => backup.Status.ToString()
        },
        StatusToTone(backup.Status));


            var actions = new List<SparkButtonItem>
{
    new SparkButtonItem
    {
        IsPrimary = true,
        IconGlyph = "👁",
        Label = "Повеќе",
        Command = new AsyncRelayCommand(() => OpenDetailsAsync(backup))
    },
    new SparkButtonItem
    {
        IconGlyph = "\uf0e2",
        Label = "Враќање на податоци",
        Command = new AsyncRelayCommand(() => RestoreAsync(backup))
    },
    new SparkButtonItem
    {
        IconGlyph = "\uf1f8",
        Label = "Избриши",
        Command = new AsyncRelayCommand(() => DeleteBackupAsync(backup))
        // ForegroundColor was an FFGridAction-only property — check whether
        // SparkButtonItem exposes an equivalent (e.g. IsDestructive, Tone,
        // ForegroundColor). If not, this row just won't be tinted red;
        // that's cosmetic, not a rendering blocker.
    }
};

            row["Actions"]=actions;

            rows.Add(row);
        }

        GridRows=rows;   // reassign, not mutate
    }
    private static SparkBadgeTone StatusToTone(BackupRunStatus status) => status switch
    {
        BackupRunStatus.Succeeded => SparkBadgeTone.Success,
        BackupRunStatus.Failed => SparkBadgeTone.Danger,
        BackupRunStatus.Running => SparkBadgeTone.Neutral,
        BackupRunStatus.Verifying => SparkBadgeTone.Warning,
        _ => SparkBadgeTone.Neutral
    };

}
