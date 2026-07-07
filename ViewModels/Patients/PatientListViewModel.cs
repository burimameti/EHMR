using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Patients;

public partial class PatientListViewModel : BaseViewModel<Patient>, IQueryAttributable
{
    // ================= SERVICES =================
    private readonly IPatientService _patientService;
    private readonly ISelectedItemService<Patient> _selectedPatient;
    private readonly INavigationService navigationService;

    // ================= FILTER STATE =================
    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();

    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedGender = "All";
    [ObservableProperty] private string selectedBloodType = "All";
    [ObservableProperty] private string selectedCity = "All";
    [ObservableProperty] private string selectedAgeGroup = "All";

    [ObservableProperty] private string filteredPatientsCount = "";
    [ObservableProperty] private ObservableCollection<Patient> filteredPatients = new();

    // ================= QUERY STATE (deep-link support) =================
    private string? _pendingSearch;
    private string? _pendingStatus;

    // ================= PERMISSIONS (base handles the actual evaluation) =================
    protected override string ModuleName => "patients";

    public ObservableCollection<string> StatusFilters { get; } = PatientFilterLookups.Status.ToObservableCollection();
    public ObservableCollection<string> GenderFilters { get; } = PatientFilterLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> BloodTypeFilters { get; } = PatientFilterLookups.BloodType.ToObservableCollection();
    public ObservableCollection<string> AgeGroups { get; } = PatientFilterLookups.AgeGroup.ToObservableCollection();
    public ObservableCollection<string> CityFilterNames
    {
        get;
    }
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();
    public string SelectedStatusDisplay
    {
        get => PatientFilterLookups.Status.ToDisplay(SelectedStatus);
        set
        {
            var internalValue = PatientFilterLookups.Status.ToInternal(value);
            if(SelectedStatus==internalValue) return;
            SelectedStatus=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedGenderDisplay
    {
        get => PatientFilterLookups.Gender.ToDisplay(SelectedGender);
        set
        {
            var internalValue = PatientFilterLookups.Gender.ToInternal(value);
            if(SelectedGender==internalValue) return;
            SelectedGender=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedBloodTypeDisplay
    {
        get => PatientFilterLookups.BloodType.ToDisplay(SelectedBloodType);
        set
        {
            var internalValue = PatientFilterLookups.BloodType.ToInternal(value);
            if(SelectedBloodType==internalValue) return;
            SelectedBloodType=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedCityDisplay
    {
        get => _cityLookup.ToDisplay(SelectedCity);
        set
        {
            var internalValue = _cityLookup.ToInternal(value);
            if(SelectedCity==internalValue) return;
            SelectedCity=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedAgeGroupDisplay
    {
        get => PatientFilterLookups.AgeGroup.ToDisplay(SelectedAgeGroup);
        set
        {
            var internalValue = PatientFilterLookups.AgeGroup.ToInternal(value);
            if(SelectedAgeGroup==internalValue) return;
            SelectedAgeGroup=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    /// <summary>Alias kept for XAML compatibility — forwards to the base class's SearchText.</summary>
    public string PatientSearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    // ================= CTOR =================
    public PatientListViewModel(
        IPatientService patientService,
        ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(navigationService, dialog, menu, authorization)
    {
        _patientService=patientService;
        _selectedPatient=selectedItemService;
        this.navigationService=navigationService;
        CityFilterNames=_cityLookup.ToObservableCollection();
        PageSize=10; // sets the BASE class's PageSize — actually drives TotalPages now

        SearchCommand=new Command<string>(query => SearchText=query);

        // keep the PatientSearchText alias in sync when SearchText changes from elsewhere (e.g. SearchCommand)
        PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SearchText))
                OnPropertyChanged(nameof(PatientSearchText));
        };

        EvaluatePermissions(); // base method, uses ModuleName
    }

    // =========================================================
    // LOAD
    // =========================================================
    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await _patientService.GetAllAsync();
        AllItems=data.ToList(); // base class owns the master list

        InitializeSparkControls();

        // apply any pending deep-link query state before the first pipeline run
        if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
        if(!string.IsNullOrWhiteSpace(_pendingStatus)) SelectedStatus=_pendingStatus;
        _pendingSearch=null;
        _pendingStatus=null;

        ApplyPipeline(); // single source of truth: search + filters + sort + paging + grid refresh
    }

    // =========================================================
    // PIPELINE HOOKS  (search and filters are now clearly separated — no overlap)
    // =========================================================
    protected override IEnumerable<Patient> ApplySearch(IEnumerable<Patient> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var s = search.Trim();
        return query.Where(x =>
            (!string.IsNullOrWhiteSpace(x.FullName)&&x.FullName.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.NationalId)&&x.NationalId.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.Phone)&&x.Phone.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.City)&&x.City.Contains(s, StringComparison.OrdinalIgnoreCase))
        );
    }

    protected override IEnumerable<Patient> ApplyFilters(IEnumerable<Patient> query)
    {
        if(SelectedStatus!="All")
            query=query.Where(x => x.Status.ToString().Equals(SelectedStatus, StringComparison.OrdinalIgnoreCase));

        if(SelectedGender!="All")
            query=query.Where(x => x.Gender.ToString().Equals(SelectedGender, StringComparison.OrdinalIgnoreCase));

        if(SelectedBloodType!="All")
            query=query.Where(x => x.BloodType.Equals(SelectedBloodType, StringComparison.OrdinalIgnoreCase));

        if(SelectedCity!="All")
            query=query.Where(x => x.City.Equals(SelectedCity, StringComparison.OrdinalIgnoreCase));

        if(SelectedAgeGroup!="All")
            query=query.Where(x => x.Age.IsInAgeGroup(SelectedAgeGroup));

        return query;
    }

    protected override IEnumerable<Patient> ApplySort(IEnumerable<Patient> query)
        => query.OrderBy(x => x.LastName);

    protected override void OnPageProjected(ObservableCollection<Patient> page)
    {
        FilteredPatients=page;                          // fires OnFilteredPatientsChanged → RefreshSparkGridRows()
        FilteredPatientsCount=$"{TotalItems} резултати"; // TotalItems comes from BaseViewModel<T>
    }

    protected override void ResetFilters()
    {
        SelectedStatus="All";
        SelectedGender="All";
        SelectedBloodType="All";
        SelectedCity="All";
        SelectedAgeGroup="All";
        SearchText="";

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedGenderDisplay));
        OnPropertyChanged(nameof(SelectedBloodTypeDisplay));
        OnPropertyChanged(nameof(SelectedCityDisplay));
        OnPropertyChanged(nameof(SelectedAgeGroupDisplay));
    }

    // =========================================================
    // QUERY ATTRIBUTES
    // =========================================================
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
    }

    // =========================================================
    // NAVIGATION / COMMANDS (unchanged)
    // =========================================================
    [RelayCommand]
    private async Task AddPatient()
    {
        _selectedPatient.SelectedItem=null;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task Select(Patient? patient) => await OpenPatient(patient);

    [RelayCommand]
    private async Task OpenPatient(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task Edit(Patient? patient)
    {
        if(patient is null) return;
        _selectedPatient.SelectedItem=patient;
        await navigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task NavigateToPatients(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(PatientSearchText)) query["search"]=PatientSearchText;
        if(!string.IsNullOrWhiteSpace(statusFilter)) query["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync(AppRoutes.Patients.List, query);
    }

    [RelayCommand]
    private async Task NavigateToAppointments(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(statusFilter)) query["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync(AppRoutes.Appointments.List, query);
    }

    [RelayCommand]
    private async Task NavigateToTherapies(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(statusFilter)) query["statusFilter"]=statusFilter;
        await Shell.Current.GoToAsync(AppRoutes.Therapy.List, query);
    }

    // ============================================================
    // TABS  (status breakdown w/ live counts — same idea as
    // DashboardViewModel.BuildSparkTabs, scoped to Patient.Status
    // since that's the dimension this page already filters on)
    // ============================================================
    private readonly Dictionary<string, SparkTabItem> _statusTabsByInternal = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabsByInternal.Clear();

        // StatusFilters already holds display strings (incl. the "All" entry,
        // e.g. "Сите"), same collection the Status picker uses — so tabs and
        // picker always agree on the available set with no duplication.
        foreach(var display in StatusFilters)
        {
            var internalValue = PatientFilterLookups.Status.ToInternal(display);

            var tab = new SparkTabItem
            {
                Title=display,
                IsSelected=SelectedStatus==internalValue
            };

            tab.Command=new RelayCommand(() => SelectTab(tab, () => SelectedStatusDisplay=display));

            Tabs.Add(tab);
            _statusTabsByInternal[internalValue]=tab;
        }

        RefreshSparkTabCounts();
    }

    /// <summary>Marks one tab selected and clears the rest, then runs the tab's own action.</summary>
    private void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs) t.IsSelected=false;
        tab.IsSelected=true;
        action();
    }

    /// <summary>
    /// Counts come from AllItems (the full unfiltered set from BaseViewModel&lt;T&gt;),
    /// not FilteredPatients — a tab should show how many patients are in that
    /// bucket overall, not how many survived the current search/filter combo.
    /// </summary>
    private void RefreshSparkTabCounts()
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
        {
            tab.Value=internalValue=="All"
                ? AllItems.Count.ToString("N0")
                : AllItems.Count(p => p.Status.ToString().Equals(internalValue, StringComparison.OrdinalIgnoreCase))
                    .ToString("N0");
        }
    }

    /// <summary>
    /// Keeps tab highlighting correct even when the status changes via the
    /// Picker instead of a tab tap — SelectedStatusDisplay's setter updates
    /// the backing SelectedStatus field, which fires this generated hook.
    /// </summary>
    partial void OnSelectedStatusChanged(string value)
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==value;
    }

    // ============================================================
    // PICKERS (entity-specific content; helper + collection live in base)
    // ============================================================
    private SparkPickerItem _statusPicker, _genderPicker, _bloodTypePicker, _cityPicker, _ageGroupPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay, s => SelectedStatusDisplay=s);
        _genderPicker=MakePicker("Пол", GenderFilters, SelectedGenderDisplay, s => SelectedGenderDisplay=s);
        _bloodTypePicker=MakePicker("Крвна група", BloodTypeFilters, SelectedBloodTypeDisplay, s => SelectedBloodTypeDisplay=s);
        _cityPicker=MakePicker("Град", CityFilterNames, SelectedCityDisplay, s => SelectedCityDisplay=s);
        _ageGroupPicker=MakePicker("Возрасна група", AgeGroups, SelectedAgeGroupDisplay, s => SelectedAgeGroupDisplay=s);

        Pickers.Add(_statusPicker);
        Pickers.Add(_genderPicker);
        Pickers.Add(_bloodTypePicker);
        Pickers.Add(_cityPicker);
        Pickers.Add(_ageGroupPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _genderPicker.SelectedItem=SelectedGenderDisplay;
        _bloodTypePicker.SelectedItem=SelectedBloodTypeDisplay;
        _cityPicker.SelectedItem=SelectedCityDisplay;
        _ageGroupPicker.SelectedItem=SelectedAgeGroupDisplay;

        // ClearFiltersCommand runs ResetFilters() → ApplyPipeline() → this method,
        // and ResetFilters sets SelectedStatus="All" via the property setter —
        // which already fires OnSelectedStatusChanged above — but calling it
        // again here is harmless and keeps tab state correct if anyone calls
        // SyncSparkPickersFromFilters() directly in the future.
        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==SelectedStatus;
    }

    // ============================================================
    // GRID  (entity-specific columns/rows)
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "ЕМБГ", Key = "NationalId", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ПАЦИЕНТ", Key = "FullName", Width = new GridLength(2.8, GridUnitType.Star) },
            new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(0.8, GridUnitType.Star) },
            new() { Header = "ВОЗРАСТ", Key = "Age", CellType = SparkGridCellType.Number, Width = new GridLength(0.9, GridUnitType.Star) },
            new() { Header = "КРВ", Key = "BloodType", Width = new GridLength(0.8, GridUnitType.Star) },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    partial void OnFilteredPatientsChanged(ObservableCollection<Patient> value) => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var p in FilteredPatients)
        {
            var row = new SparkGridRow { Tag=p };
            row["NationalId"]=p.NationalId;
            row["FullName"]=p.FullName;
            row["Gender"]=p.Gender.ToString();
            row["Age"]=p.Age;
            row["BloodType"]=p.BloodType;
            row["Phone"]=p.Phone;
            row["Status"]=new SparkBadgeValue(p.Status.ToString(), StatusToTone(p.Status.ToString()));
            rows.Add(row);
        }
        GridRows=rows;
    }

    private static SparkBadgeTone StatusToTone(string status) => status?.ToLowerInvariant() switch
    {
        "active" => SparkBadgeTone.Success,
        "inactive" or "discharged" or "missed" => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

    // ============================================================
    // WIRING
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();   // inherited from BaseViewModel<T>
        BuildSparkGridColumns();
    }
}