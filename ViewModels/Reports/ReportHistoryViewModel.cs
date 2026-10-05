using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;

using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Reports;

public partial class ReportHistoryViewModel
    : BaseViewModel<ReportHistory>, IQueryAttributable
{
    private readonly IReportHistoryService _reportService;

    protected override string ModuleName => "reports";

    [ObservableProperty] private ObservableCollection<ReportHistory> filteredReports = new();
    [ObservableProperty] private string filteredReportsCount = "";
    [ObservableProperty] private string selectedFormat = "All";
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedUser = "All";

    public ObservableCollection<string> FormatFilters { get; } = ["All", "PDF", "Excel"];
    public ObservableCollection<string> StatusFilters { get; } = ["All", "Success", "Failed"];
    public ObservableCollection<string> UserFilters { get; } = new();
    private static readonly Dictionary<string, string> FormatLabels = new()
    {
        ["All"]="Сите",
        ["PDF"]="PDF",
        ["Excel"]="Excel"
    };

    private static readonly Dictionary<string, string> StatusLabels = new()
    {
        ["All"]="Сите",
        ["Success"]="Успешно",
        ["Failed"]="Неуспешно"
    };

    private static readonly Dictionary<string, string> UserLabels = new()
    {
        ["All"]="Сите"
    };
    public string ReportSearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    private string? _pendingSearch;
    private string? _pendingFormat;
    private readonly INavigationService _navigationService;
    public ReportHistoryViewModel(
        IReportHistoryService reportService,
        INavigationService navigation,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization,
        ISelectedItemService<ReportHistory> selectedItemService)
        : base(navigation, dialog, menu, authorization, selectedItemService)
    {
        _reportService=reportService;
        _navigationService=navigation;
        PageSize=15;

        SearchCommand=new Command<string>(q => SearchText=q);

        PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SearchText))
                OnPropertyChanged(nameof(ReportSearchText));
        };

        EvaluatePermissions();
    }

    // ============================================================
    // LOAD
    // ============================================================
    [RelayCommand]
    public async Task LoadAsync()
        => await ExecuteSafeAsync(LoadCoreAsync, "Грешка при вчитување на историјата на извештаи");

    private async Task LoadCoreAsync()
    {
        // Re-evaluate after authentication/role has been established.
        // The ViewModel is transient, while AuthStateService is singleton.
        EvaluatePermissions();

        var reports = await _reportService.GetAllAsync();
        AllItems=reports.ToList();

        UserFilters.Clear();
        UserFilters.Add("All");

        foreach(var user in reports
                     .Select(x => x.GeneratedBy)
                     .Where(x => !string.IsNullOrWhiteSpace(x))
                     .Distinct()
                     .OrderBy(x => x))
        {
            UserFilters.Add(user!);
        }

        InitializeSparkControls();

        if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
        if(!string.IsNullOrWhiteSpace(_pendingFormat)) SelectedFormat=_pendingFormat;

        _pendingSearch=null;
        _pendingFormat=null;

        ApplyPipeline();
    }

    protected override IEnumerable<ReportHistory> ApplySearch(IEnumerable<ReportHistory> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        search=search.Trim();

        return query.Where(x =>
            (!string.IsNullOrWhiteSpace(x.ReportTitle)&&x.ReportTitle.Contains(search, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.GeneratedBy)&&x.GeneratedBy.Contains(search, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.FileName)&&x.FileName.Contains(search, StringComparison.OrdinalIgnoreCase))
        );
    }

    protected override IEnumerable<ReportHistory> ApplyFilters(IEnumerable<ReportHistory> query)
    {
        if(SelectedFormat!="All")
            query=query.Where(x => x.Format==SelectedFormat);

        if(SelectedStatus!="All")
        {
            bool success = SelectedStatus=="Success";
            query=query.Where(x => x.Success==success);
        }

        if(SelectedUser!="All")
            query=query.Where(x => x.GeneratedBy==SelectedUser);

        return query;
    }

    protected override IEnumerable<ReportHistory> ApplySort(IEnumerable<ReportHistory> query)
        => query.OrderByDescending(x => x.GeneratedOn);

    protected override void OnPageProjected(ObservableCollection<ReportHistory> page)
    {
        FilteredReports=page;
        FilteredReportsCount=$"{TotalItems} репорти";
    }

    protected override void ResetFilters()
    {
        SelectedFormat="All";
        SelectedStatus="All";
        SelectedUser="All";
        SearchText="";
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingFormat=query.TryGetValue("format", out var f) ? f?.ToString() : null;
    }

    // ============================================================
    // TABS
    // ============================================================
    private readonly Dictionary<string, SparkTabItem> _tabsByFormat = new();

    private void BuildSparkTabs()
    {
        BuildTabFilters(
            options: FormatFilters,
            keySelector: f => f,
            labelSelector: f => FormatLabels.TryGetValue(f, out var lbl) ? lbl : f,
            isSelectedSelector: f => f==SelectedFormat,
            onSelect: f => SelectedFormat=f);

        RefreshSparkTabCounts();
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var format in FormatFilters)
        {
            RefreshTabCount(
                format,
                format=="All"
                    ? AllItems.Count
                    : AllItems.Count(x => x.Format==format));
        }
    }

    partial void OnSelectedFormatChanged(string value)
    {
        SyncTabsFromKey(value);
        ApplyPipeline();
    }

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _formatPicker;
    private SparkPickerItem _statusPicker;
    private SparkPickerItem _userPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();

        _formatPicker=MakeLocalizedPicker(
            "Формат", FormatFilters, SelectedFormat,
            s => SelectedFormat=s,
            FormatLabels);

        _statusPicker=MakeLocalizedPicker(
            "Статус", StatusFilters, SelectedStatus,
            s => { SelectedStatus=s; ApplyPipeline(); },
            StatusLabels);

        _userPicker=MakeLocalizedPicker(
            "Генериран од", UserFilters, SelectedUser,
            s => { SelectedUser=s; ApplyPipeline(); },
            UserLabels);

        Pickers.Add(_formatPicker);
        Pickers.Add(_statusPicker);
        Pickers.Add(_userPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_formatPicker==null) return;

        _formatPicker.SelectedItem=FormatLabels.TryGetValue(SelectedFormat, out var fLbl) ? fLbl : SelectedFormat;
        _statusPicker.SelectedItem=StatusLabels.TryGetValue(SelectedStatus, out var sLbl) ? sLbl : SelectedStatus;
        _userPicker.SelectedItem=UserLabels.TryGetValue(SelectedUser, out var uLbl) ? uLbl : SelectedUser;

        SyncTabsFromKey(SelectedFormat);
    }

    // ============================================================
    // GRID
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
   
                { new() { Header = "БРОЈ", Key = "ReportNumber", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header="ИЗВЕШТАЈ", Key="Report", Width=new GridLength(2.5, GridUnitType.Star) },
            new() { Header="ФОРМАТ", Key="Format", Width=new GridLength(1, GridUnitType.Star) },
            new() { Header="КОРИСНИК", Key="GeneratedBy", Width=new GridLength(1.5, GridUnitType.Star) },
            new() { Header="ДАТУМ ГЕНЕРИРАН", Key="GeneratedOn", Width=new GridLength(1.5, GridUnitType.Star) },
            new() { Header="ГОЛЕМИНА", Key="FileSize", Width=new GridLength(1, GridUnitType.Star) },
            new() { Header="СТАТУС", Key="Status", CellType=SparkGridCellType.Badge, Width=new GridLength(1, GridUnitType.Star) },
            new() { Header="ОПЕРАЦИИ", Key="Actions", CellType=SparkGridCellType.Actions, Width=GridLength.Auto }
        };
    }

    partial void OnFilteredReportsChanged(ObservableCollection<ReportHistory> value)
        => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var report in FilteredReports)
        {
            var row = new SparkGridRow { Tag=report };
            row["ReportNumber"]=report.ReportNumber;
            row["Report"]=report.ReportTitle;
            row["Format"]=report.Format;
            row["GeneratedBy"]=report.GeneratedBy;
            row["GeneratedOn"]=report.GeneratedOn.ToString("dd.MM.yyyy HH:mm");
            row["FileSize"]=$"{report.FileSize/1024d:0.##} KB";
            row["Status"]=new SparkBadgeValue(report.Success ? "Успешно" : "Не успешно", StatusToTone(report.Success));

            row["Actions"]=new List<SparkButtonItem>
            {
                new SparkButtonItem { IsPrimary=true, IconGlyph="📂", Label="Отвори", Command=OpenCommand, CommandParameter=report },
                new SparkButtonItem { IconGlyph="⬇", Label="Превземи", Command=DownloadCommand, CommandParameter=report },
                new SparkButtonItem { IconGlyph="🗑", Label="Избриши", Command=DeleteCommand, CommandParameter=report, IsPrimary=false }
            };

            rows.Add(row);
        }

        GridRows=rows;
    }

    private static SparkBadgeTone StatusToTone(bool success)
        => success ? SparkBadgeTone.Success : SparkBadgeTone.Danger;

    // ============================================================
    // INITIALIZATION
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }

    // ============================================================
    // COMMANDS
    // ============================================================
    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task ClearHistory()
    {
        bool confirm = await UserDialogService.ShowConfirmationAsync(
            "Чистење Историја",
            "Дали сакате да чистите историја?",
            "Да", "Не");

        if(!confirm) return;

        await _reportService.ClearAsync();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Download(ReportHistory? report)
    {
        if(report==null) return;

        if(!File.Exists(report.FilePath))
        {
            await UserDialogService.ShowAlertAsync("Датотека не е пронајдена", "Избраниот извечтајот не постои.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest { File=new ReadOnlyFile(report.FilePath) });
    }

    [RelayCommand]
    private async Task Open(ReportHistory? report)
    {
        if(report==null) return;

        if(!File.Exists(report.FilePath))
        {
            await UserDialogService.ShowAlertAsync("Датотека не е пронајдена", "Извечтајот не може да се пронајде.", "OK");
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest { File=new ReadOnlyFile(report.FilePath) });
    }

    [RelayCommand]
    private async Task OpenFolder(ReportHistory? report)
    {
        if(report==null) return;
        if(!File.Exists(report.FilePath)) return;

#if WINDOWS
        System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo
            {
                FileName="explorer.exe",
                Arguments=$"/select,\"{report.FilePath}\"",
                UseShellExecute=true
            });
#endif

        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Delete(ReportHistory? report)
    {
        if(report==null) return;

        var confirm = await UserDialogService.ShowPromptAsync(
            "избриши Извештај Report",
            $"Избриши '{report.ReportTitle}' ?",
            "Избриши", "Откажи");

        if(confirm=="Откажи") return;

        await _reportService.DeleteAsync(report.Id);
        await LoadAsync();
    }
    [RelayCommand]
    private async void NavigateToReport()
    {
        EvaluatePermissions();

        if(!CanCreate)
        {
            await UserDialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате авторизација за креирање извештај.",
                "OK");
            return;
        }

        await _navigationService.GoToAsync(AppRoutes.Reports.Detail);
    }
    // ================= SELECT — overridden to open the file, not navigate =================
    protected override async Task Select(ReportHistory item)
    {
        if(item is null) return;
        await Open(item);
    }
    private SparkPickerItem MakeLocalizedPicker(
        string placeholder,
        IEnumerable<string> keys,
        string initialKey,
        Action<string> onSelected,
        IReadOnlyDictionary<string, string> labelMap)
    {
        // key -> label (for display) and label -> key (to translate selection back)
        var keyByLabel = new Dictionary<string, string>();
        var labels = new List<string>();

        foreach(var key in keys)
        {
            var label = labelMap.TryGetValue(key, out var mapped) ? mapped : key;
            labels.Add(label);
            keyByLabel[label]=key;
        }

        var initialLabel = labelMap.TryGetValue(initialKey, out var initMapped) ? initMapped : initialKey;

        return MakePicker(placeholder, labels, initialLabel, selectedLabel =>
        {
            if(keyByLabel.TryGetValue(selectedLabel, out var key))
                onSelected(key);
            else
                onSelected(selectedLabel); // fallback, shouldn't normally happen
        });
    }

}


