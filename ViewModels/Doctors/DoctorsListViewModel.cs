using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Doctors.Extensions;

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class DoctorsListViewModel
    : BaseViewModel<Doctor>, IQueryAttributable
{
    // ============================================================
    // SERVICES
    // ============================================================

    private readonly IDoctorService _doctorService;

    // ============================================================
    // FILTER STATE
    // ============================================================

    [ObservableProperty]
    private string selectedStatus = "All";

    [ObservableProperty]
    private string filteredDoctorsCount = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Doctor> filteredDoctors = new();

    // ============================================================
    // QUERY STATE
    // ============================================================

    private string? _pendingSearch;
    private string? _pendingStatus;

    // ============================================================
    // BASE OVERRIDES
    // ============================================================

    protected override string ModuleName => Modules.Doctors;

    protected override string DetailRoute =>
        AppRoutes.Doctors.Detail;

    protected override string PermissionDeniedMessage =>
        "Немате авторизација за додавање нов реуматолог.";

    // ============================================================
    // STATUS FILTER
    // ============================================================

    public ObservableCollection<string> StatusFilters
    {
        get;
    } =
        DoctorFilterLookups.Status.ToObservableCollection();

    public string SelectedStatusDisplay
    {
        get => DoctorFilterLookups.Status.ToDisplay(SelectedStatus);

        set
        {
            var internalValue =
                DoctorFilterLookups.Status.ToInternal(value);

            if(SelectedStatus==internalValue)
                return;

            SelectedStatus=internalValue;

            ApplyPipeline();

            OnPropertyChanged();
        }
    }

    // ============================================================
    // SEARCH
    // ============================================================

    public string DoctorSearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    // ============================================================
    // CTOR
    // ============================================================

    public DoctorsListViewModel(
        IDoctorService doctorService,
        ISelectedItemService<Doctor> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(
            navigationService,
            dialog,
            menu,
            authorization,
            selectedItemService)
    {
        _doctorService=doctorService;

        PageSize=10;

        SearchCommand=
            new Command<string>(
                query => SearchText=query??string.Empty);

        PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SearchText))
            {
                OnPropertyChanged(
                    nameof(DoctorSearchText));
            }
        };

        EvaluatePermissions();
    }

    // ============================================================
    // LOAD
    // ============================================================

    [RelayCommand]
    public async Task LoadAsync()
    {
        var data =
            await _doctorService.GetAllAsync();

        AllItems=data
            .Where(x => x is not null)
            .ToList();

        InitializeSparkControls();

        if(!string.IsNullOrWhiteSpace(_pendingSearch))
        {
            SearchText=_pendingSearch;
        }

        if(!string.IsNullOrWhiteSpace(_pendingStatus))
        {
            SelectedStatus=_pendingStatus;
        }

        _pendingSearch=null;
        _pendingStatus=null;

        ApplyPipeline();
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    [RelayCommand]
    private async Task OpenDetail(object tag)
    {
        if(tag is not Doctor doctor)
            return;

        SelectedItemService.SelectedItem=doctor;

        await NavigationService.GoToAsync(
            AppRoutes.Doctors.Detail);
    }

    // ============================================================
    // SEARCH
    // ============================================================

    protected override IEnumerable<Doctor> ApplySearch(
        IEnumerable<Doctor> query,
        string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim();

        return query.Where(d =>
            (!string.IsNullOrWhiteSpace(d.FullName)&&
             d.FullName.Contains(
                 term,
                 StringComparison.OrdinalIgnoreCase))
            ||
            (!string.IsNullOrWhiteSpace(d.DoctorNumber)&&
             d.DoctorNumber.Contains(
                 term,
                 StringComparison.OrdinalIgnoreCase))
            ||
            (!string.IsNullOrWhiteSpace(d.ContactPhone)&&
             d.ContactPhone.Contains(
                 term,
                 StringComparison.OrdinalIgnoreCase))
            ||
            (!string.IsNullOrWhiteSpace(d.Email)&&
             d.Email.Contains(
                 term,
                 StringComparison.OrdinalIgnoreCase)));
    }

    // ============================================================
    // FILTER
    // ============================================================

    protected override IEnumerable<Doctor> ApplyFilters(
        IEnumerable<Doctor> query)
    {
        return SelectedStatus switch
        {
            "Active" =>
                query.Where(
                    x => x.Status==Status.Active),

            "Inactive" =>
                query.Where(
                    x => x.Status==Status.Inactive),

            _ => query
        };
    }

    // ============================================================
    // SORT
    // ============================================================

    protected override IEnumerable<Doctor> ApplySort(
        IEnumerable<Doctor> query)
    {
        return query
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.DoctorNumber);
    }

    // ============================================================
    // PAGE RESULT
    // ============================================================

    protected override void OnPageProjected(
        ObservableCollection<Doctor> page)
    {
        FilteredDoctors=page;

        FilteredDoctorsCount=
            $"{TotalItems} резултати";
    }

    // ============================================================
    // RESET
    // ============================================================

    protected override void ResetFilters()
    {
        SelectedStatus="All";
        SearchText=string.Empty;

        OnPropertyChanged(
            nameof(SelectedStatusDisplay));
    }

    // ============================================================
    // QUERY ATTRIBUTES
    // ============================================================

    public void ApplyQueryAttributes(
        IDictionary<string, object> query)
    {
        _pendingSearch=
            query.TryGetValue(
                "search",
                out var search)
                ? search?.ToString()
                : null;

        _pendingStatus=
            query.TryGetValue(
                "statusFilter",
                out var status)
                ? status?.ToString()
                : null;
    }

    // ============================================================
    // PROPERTY CHANGES
    // ============================================================

    partial void OnSelectedStatusChanged(
        string value)
    {
        foreach(var (internalValue, tab)
                in _statusTabsByInternal)
        {
            tab.IsSelected=
                internalValue==value;
        }
    }

    partial void OnFilteredDoctorsChanged(
        ObservableCollection<Doctor> value)
    {
        RefreshSparkGridRows();
    }

    // ============================================================
    // TABS
    // ============================================================

    private readonly Dictionary<string, SparkTabItem>
        _statusTabsByInternal = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();

        _statusTabsByInternal.Clear();

        foreach(var display in StatusFilters)
        {
            var internalValue =
                DoctorFilterLookups.Status
                    .ToInternal(display);

            var tab = new SparkTabItem
            {
                Title=display,

                IsSelected=
                    SelectedStatus==internalValue
            };

            tab.Command=
                new RelayCommand(
                    () => SelectTab(
                        tab,
                        () =>
                            SelectedStatusDisplay=
                                display));

            Tabs.Add(tab);

            _statusTabsByInternal[
                internalValue]=tab;
        }
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var (internalValue, tab)
                in _statusTabsByInternal)
        {
            tab.Value=internalValue switch
            {
                "All" =>
                    AllItems.Count
                        .ToString("N0"),

                "Active" =>
                    AllItems.Count(
                        d =>
                            d.Status==
                            Status.Active)
                        .ToString("N0"),

                "Inactive" =>
                    AllItems.Count(
                        d =>
                            d.Status==
                            Status.Inactive)
                        .ToString("N0"),

                _ => "0"
            };
        }
    }

    // ============================================================
    // PICKERS
    // ============================================================

    private void BuildSparkPickers()
    {
        Pickers.Clear();
    }

    protected override void SyncSparkPickersFromFilters()
    {
    }

    // ============================================================
    // GRID
    // ============================================================

    [ObservableProperty]
    private ObservableCollection<SparkGridColumn>
        gridColumns = new();

    [ObservableProperty]
    private ObservableCollection<SparkGridRow>
        gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=
            new ObservableCollection<SparkGridColumn>
            {
                new()
                {
                    Header = "БРОЈ",
                    Key = "DoctorNumber",
                    Width =
                        new GridLength(
                            1.3,
                            GridUnitType.Star)
                },

                new()
                {
                    Header = "РЕУМАТОЛОГ",
                    Key = "FullName",
                    Width =
                        new GridLength(
                            2.2,
                            GridUnitType.Star),
                    CellType =
                        SparkGridCellType.Hyperlink
                },

                new()
                {
                    Header = "Е-ПОШТА",
                    Key = "Email",
                    Width =
                        new GridLength(
                            2,
                            GridUnitType.Star)
                },

                new()
                {
                    Header = "ТЕЛЕФОН",
                    Key = "ContactPhone",
                    Width =
                        new GridLength(
                            1.3,
                            GridUnitType.Star)
                },

                new()
                {
                    Header = "СТАТУС",
                    Key = "Status",
                    Width =
                        new GridLength(
                            1,
                            GridUnitType.Star),
                    CellType =
                        SparkGridCellType.Badge
                },

                new()
                {
                    Header = "ОПЦИИ",
                    Key = "Actions",
                    Width = GridLength.Auto,
                    CellType =
                        SparkGridCellType.Actions
                }
            };
    }

    private void RefreshSparkGridRows()
    {
        var rows =
            new ObservableCollection<SparkGridRow>();

        foreach(var doctor in FilteredDoctors)
        {
            var isActive =
                doctor.Status==Status.Active;

            var row =
                new SparkGridRow
                {
                    Tag=doctor
                };

            row["DoctorNumber"]=
                string.IsNullOrWhiteSpace(
                    doctor.DoctorNumber)
                    ? "—"
                    : doctor.DoctorNumber;

            row["FullName"]=
                string.IsNullOrWhiteSpace(
                    doctor.FullName)
                    ? "—"
                    : doctor.FullName;

            row["Email"]=
                string.IsNullOrWhiteSpace(
                    doctor.Email)
                    ? "—"
                    : doctor.Email;

            row["ContactPhone"]=
                string.IsNullOrWhiteSpace(
                    doctor.ContactPhone)
                    ? "—"
                    : doctor.ContactPhone;

            row["Status"]=
                new SparkBadgeValue(
                    isActive
                        ? "Активен"
                        : "Неактивен",

                    isActive
                        ? SparkBadgeTone.Success
                        : SparkBadgeTone.Danger);

            AddDefaultActions(
                doctor,
                row,
                detailLabel: "Детали",
                editLabel: "Промени");

            rows.Add(row);
        }

        GridRows=rows;
    }

    // ============================================================
    // WIRING
    // ============================================================

    private void InitializeSparkControls()
    {
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }
}