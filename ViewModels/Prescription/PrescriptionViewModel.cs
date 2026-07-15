using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionListViewModel : BaseViewModel<Prescription>, IQueryAttributable
{
    // ================= SERVICES =================
    private readonly IPrescriptionService _prescriptionService;

    // ================= FILTER STATE =================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string filteredPrescriptionsCount = "";
    [ObservableProperty] private ObservableCollection<Prescription> filteredPrescriptions = new();

    // ================= QUERY STATE (deep-link support) =================
    private string? _pendingSearch;
    private string? _pendingStatus;

    // ================= PERMISSIONS / NAVIGATION =================
    protected override string ModuleName => "prescriptions";
    protected override string DetailRoute => AppRoutes.Prescriptions.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за оваа акција со рецепти.";

    public ObservableCollection<string> StatusFilters { get; } = new() { "Сите", "Активни", "Реализирани", "Повлечени" };

    public string SelectedStatusDisplay
    {
        get => SelectedStatus=="All" ? "Сите" : SelectedStatus;
        set
        {
            var internalValue = value=="Сите" ? "All" : value;
            if(SelectedStatus==internalValue) return;
            SelectedStatus=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    /// <summary>Alias за компатибилност со XAML.</summary>
    public string PrescriptionSearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    // ================= CTOR =================
    public PrescriptionListViewModel(
        IPrescriptionService prescriptionService,
        ISelectedItemService<Prescription> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(navigationService, dialog, menu, authorization, selectedItemService)
    {
        _prescriptionService=prescriptionService;

        PageSize=10;
        SearchCommand=new Command<string>(query => SearchText=query);

        PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SearchText))
                OnPropertyChanged(nameof(PrescriptionSearchText));
        };

        EvaluatePermissions();
    }

    // =========================================================
    // LOAD
    // =========================================================
    [RelayCommand]
    public async Task LoadAsync()
        => await ExecuteSafeAsync(LoadCoreAsync, "Грешка при вчитување на рецептите");

    private async Task LoadCoreAsync()
    {
        var data = await _prescriptionService.GetAllAsync();
        AllItems=data.ToList();

        InitializeSparkControls();

        if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
        if(!string.IsNullOrWhiteSpace(_pendingStatus)) SelectedStatus=_pendingStatus;
        _pendingSearch=null;
        _pendingStatus=null;

        ApplyPipeline();
    }

    // =========================================================
    // PIPELINE HOOKS
    // =========================================================
    protected override IEnumerable<Prescription> ApplySearch(IEnumerable<Prescription> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var s = search.Trim();
        return query.Where(x =>
            (!string.IsNullOrWhiteSpace(x.Medication)&&x.Medication.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.Instructions)&&x.Instructions.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (x.Patient!=null&&!string.IsNullOrWhiteSpace(x.Patient.FullName)&&x.Patient.FullName.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (x.Patient!=null&&!string.IsNullOrWhiteSpace(x.Patient.NationalId)&&x.Patient.NationalId.Contains(s, StringComparison.OrdinalIgnoreCase))
        );
    }

    protected override IEnumerable<Prescription> ApplyFilters(IEnumerable<Prescription> query)
    {
        if(SelectedStatus!="All")
        {
            query=query.Where(x => !string.IsNullOrWhiteSpace(x.Status)&&
                                       x.Status.Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    protected override IEnumerable<Prescription> ApplySort(IEnumerable<Prescription> query)
        => query.OrderByDescending(x => x.Id);

    protected override void OnPageProjected(ObservableCollection<Prescription> page)
    {
        FilteredPrescriptions=page;
        FilteredPrescriptionsCount=$"{TotalItems} резултати";
    }

    protected override void ResetFilters()
    {
        SelectedStatus="All";
        SearchText="";
        OnPropertyChanged(nameof(SelectedStatusDisplay));
    }

    // =========================================================
    // QUERY ATTRIBUTES
    // =========================================================
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
    }

    // ============================================================
    // TABS  (base веќе носи TabsByKey / SelectTab / RefreshTabCount / SyncTabsFromKey)
    // ============================================================
    private void BuildSparkTabs()
    {
        BuildTabFilters(
            options: StatusFilters,
            keySelector: display => display=="Сите" ? "All" : display,
            labelSelector: display => display,
            isSelectedSelector: display => (display=="Сите" ? "All" : display)==SelectedStatus,
            onSelect: display => SelectedStatusDisplay=display);

        RefreshSparkTabCounts();
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var display in StatusFilters)
        {
            var internalValue = display=="Сите" ? "All" : display;

            RefreshTabCount(
                internalValue,
                internalValue=="All"
                    ? AllItems.Count
                    : AllItems.Count(p => !string.IsNullOrWhiteSpace(p.Status)&&
                                           p.Status.Equals(internalValue, StringComparison.OrdinalIgnoreCase)));
        }
    }

    partial void OnSelectedStatusChanged(string value)
        => SyncTabsFromKey(value);

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _statusPicker = null!;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay, s => SelectedStatusDisplay=s);
        Pickers.Add(_statusPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;

        SyncTabsFromKey(SelectedStatus);
    }

    // ============================================================
    // GRID MAPPING
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ПАЦИЕНТ", Key = "PatientName", Width = new GridLength(2.0, GridUnitType.Star) },
            new() { Header = "МЕДИКАМЕНТ", Key = "Medication", Width = new GridLength(2.0, GridUnitType.Star) },
            new() { Header = "ДОЗИРАЊЕ", Key = "Dosage", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "ИНСТРУКЦИИ", Key = "Instructions", Width = new GridLength(3.0, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    partial void OnFilteredPrescriptionsChanged(ObservableCollection<Prescription> value) => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var p in FilteredPrescriptions)
        {
            var row = new SparkGridRow { Tag=p };
            row["PatientName"]=p.Patient?.FullName??"Непознат Пациент";
            row["Medication"]=p.Medication??"/";
            row["Dosage"]=p.Dosage??"/";
            row["Instructions"]=p.Instructions??"/";

            var actions = new List<SparkButtonItem>();

            if(CanUpdate)
                actions.Add(new SparkButtonItem { Label="✎", Command=EditCommand, CommandParameter=p });

            row["Actions"]=actions;

            rows.Add(row);
        }
        GridRows=rows;
    }

    // ============================================================
    // WIRING
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }
}