using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels.Patients;

public partial class PatientListViewModel : BaseViewModel<Patient>, IQueryAttributable
{
    // ================= SERVICES =================
    private readonly IPatientService _patientService;

    [ObservableProperty] private SparkGridRow selectedPatientRow;

    // ================= FILTER STATE =================
    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();
    protected override Func<Patient, Guid?>? DoctorOwnerSelector => p => p.DoctorId;
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedGender = "All";
    [ObservableProperty] private string selectedBloodType = "All";
    [ObservableProperty] private string selectedCity = "All";
    [ObservableProperty] private string selectedAgeGroup = "All";

    [ObservableProperty] private string filteredPatientCount = "";
    [ObservableProperty] private bool useCyrillicSearch = true;
    private bool _sparkInitialized;
    // ================= QUERY STATE (deep-link support) =================
    private string? _pendingSearch;
    private string? _pendingStatus;

    // ================= BASE OVERRIDES =================
    protected override string ModuleName => Modules.Patients;
    protected override string DetailRoute => AppRoutes.Patients.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нов пациент.";

    public ObservableCollection<string> StatusFilters { get; } = PatientFilterLookups.Status.ToObservableCollection();
    public ObservableCollection<string> GenderFilters { get; } = PatientFilterLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> BloodTypeFilters { get; } = PatientFilterLookups.BloodType.ToObservableCollection();
    public ObservableCollection<string> AgeGroups { get; } = PatientFilterLookups.AgeGroup.ToObservableCollection();
    public ObservableCollection<string> CityFilterNames
    {
        get;
    }

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
        : base(navigationService, dialog, menu, authorization, selectedItemService)
    {
        _patientService=patientService;
        CityFilterNames=_cityLookup.ToObservableCollection();
        PageSize=10;

        SearchCommand=new Command<string>(query => SearchText=query);

        EvaluatePermissions();
    }

    // =========================================================
    // LOAD
    // =========================================================
    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            var data=await _patientService.GetAllBaseAsync();
            AllItems=data.ToList();

            if(!_sparkInitialized)
            {
                InitializeSparkControls();
                _sparkInitialized=true;
            }

            if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
            if(!string.IsNullOrWhiteSpace(_pendingStatus)) SelectedStatus=_pendingStatus;
            _pendingSearch=null;
            _pendingStatus=null;

            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Неуспешно вчитување на пациентите: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // PIPELINE HOOKS
    // =========================================================
    protected override IEnumerable<Patient> ApplySearch(IEnumerable<Patient> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var tokens=s.Split(' ', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
        var cyrillicSearch=UseCyrillicSearch
            ? MacedonianTransliterator.ToCyrillic(s)
            : s;
        var cyrTokens=UseCyrillicSearch
            ? tokens.Select(MacedonianTransliterator.ToCyrillic).ToArray()
            : tokens;

        return query.Where(x =>
        {
            var first=x.FirstName??string.Empty;
            var last=x.LastName??string.Empty;
            var full=$"{first} {last}".Trim();

            if(tokens.Length>1)
            {
                return StartsWith(first,tokens[0],cyrTokens[0])
                    &&StartsWith(last,tokens[1],cyrTokens[1]);
            }

            return StartsWith(first,s,cyrillicSearch)
                ||StartsWith(last,s,cyrillicSearch)
                ||StartsWith(full,s,cyrillicSearch)
                ||StartsWith(x.NationalId,s,cyrillicSearch)
                ||StartsWith(x.SzboNumber,s,cyrillicSearch)
                ||StartsWith(x.Phone,s,cyrillicSearch)
                ||StartsWith(x.City,s,cyrillicSearch);
        });

        static bool StartsWith(string? value,string latin,string alternate)
            => !string.IsNullOrWhiteSpace(value)
                &&(value.StartsWith(latin,StringComparison.OrdinalIgnoreCase)
                   ||value.StartsWith(alternate,StringComparison.OrdinalIgnoreCase));
    }

    partial void OnUseCyrillicSearchChanged(bool value) => ApplyPipeline();
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
        filteredPatientCount=$"{TotalItems} резултати";
        RefreshSparkGridRows(page);
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
    // NAVIGATION HELPERS (entity-specific, not covered by base Select/New/Edit)
    // =========================================================
    [RelayCommand]
    private async Task NavigateToPatients(string? statusFilter = null)
    {
        var query = new Dictionary<string, object>();
        if(!string.IsNullOrWhiteSpace(SearchText)) query["search"]=SearchText;
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

    [RelayCommand]
    private async Task NewEncounterForSelected(Patient? patient)
    {
        if(patient is null)
        {
            await UserDialogService.ShowAlertAsync("Внимание", "Одберете пациент прво.");
            return;
        }

        if(patient.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "За неактивен пациент не може да се креира нов преглед.", "ОК");
            return;
        }

        SelectedItemService.SelectedItem=patient;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Create);
    }
     [RelayCommand]
    private async Task AddAsync()
    {      
        SelectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }
    // ============================================================
    // TABS
    // ============================================================
    private readonly Dictionary<string, SparkTabItem> _statusTabsByInternal = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabsByInternal.Clear();

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

    partial void OnSelectedStatusChanged(string value)
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==value;
    }

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _statusPicker, _genderPicker, _bloodTypePicker, _cityPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _statusPicker=MakePicker("Статус", StatusFilters, SelectedStatusDisplay, s => SelectedStatusDisplay=s);
        _genderPicker=MakePicker("Пол", GenderFilters, SelectedGenderDisplay, s => SelectedGenderDisplay=s);
        _bloodTypePicker=MakePicker("Крвна група", BloodTypeFilters, SelectedBloodTypeDisplay, s => SelectedBloodTypeDisplay=s);
        _cityPicker=MakePicker("Град", CityFilterNames, SelectedCityDisplay, s => SelectedCityDisplay=s);
        //_ageGroupPicker=MakePicker("Возрасна група", AgeGroups, SelectedAgeGroupDisplay, s => SelectedAgeGroupDisplay=s);

        Pickers.Add(_statusPicker);
        Pickers.Add(_genderPicker);
        Pickers.Add(_bloodTypePicker);
        Pickers.Add(_cityPicker);
        //Pickers.Add(_ageGroupPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_statusPicker==null) return;
        _statusPicker.SelectedItem=SelectedStatusDisplay;
        _genderPicker.SelectedItem=SelectedGenderDisplay;
        _bloodTypePicker.SelectedItem=SelectedBloodTypeDisplay;
        _cityPicker.SelectedItem=SelectedCityDisplay;
        //_ageGroupPicker.SelectedItem=SelectedAgeGroupDisplay;

        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==SelectedStatus;
    }

    // ============================================================
    // GRID
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
                 new() { Header = "БРОЈ НА ПАЦИЕНТ", Key = "PatientNumber", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ЕЗБО БРОЈ", Key = "SzboNumber", Width = new GridLength(1.25, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "SzboNumber", Width = new GridLength(1.25, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "FullName", Width = new GridLength(2.8, GridUnitType.Star) },
            new() { Header = "ПОЛ", Key = "Gender", Width = new GridLength(0.8, GridUnitType.Star) },
            new() { Header = "ВОЗРАСТ", Key = "Age", CellType = SparkGridCellType.Number, Width = new GridLength(0.9, GridUnitType.Star) },
            new() { Header = "КРВ", Key = "BloodType", Width = new GridLength(0.8, GridUnitType.Star) },
            new() { Header = "ТЕЛЕФОН", Key = "Phone", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "НОВ ПРЕГЛЕД", Key = "Pregled", CellType = SparkGridCellType.Button, Width = new GridLength(1.4, GridUnitType.Star) },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    private void RefreshSparkGridRows(IEnumerable<Patient> patients)
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var p in patients)
        {
            var row = new SparkGridRow { Tag=p };
            row["PatientNumber"]=p.PatientNumber;
            row["SzboNumber"]=p.SzboNumber;
            row["FullName"]=p.FullName;
            row["Gender"]=p.Gender.ToDisplay();
            row["Age"]=p.Age;
            row["BloodType"]=p.BloodType;
            row["Phone"]=p.Phone;
            row["Status"]=new SparkBadgeValue(p.Status.ToDisplay(), StatusToTone(p.Status));

            AddDefaultActions(p, row, detailLabel: "Повеќе", editLabel: "Промени", canEditPredicate: x => x.Status==PatientStatus.Active);

            if(p.Status==PatientStatus.Active)
            {
                row["Pregled"]=new SparkButtonItem
                {
                    IconGlyph="\uD83D\uDCC5",
                    Label="Нов преглед",
                    IsPrimary=true,
                    Command=NewEncounterForSelectedCommand,
                    CommandParameter=p
                };
            }
            else
            {
                row["Pregled"]=new SparkButtonItem
                {
                    IconGlyph="\ud83d\udd12",
                    Label="Заклучено",
                    IsPrimary=false
                };
            }

            rows.Add(row);
        }
        GridRows=rows;
    }

    private static SparkBadgeTone StatusToTone(PatientStatus status) => status switch
    {
        PatientStatus.Active => SparkBadgeTone.Success,
        PatientStatus.Inactive => SparkBadgeTone.Danger,
        PatientStatus.Chronic => SparkBadgeTone.Warning,
        PatientStatus.Deceased => SparkBadgeTone.Danger,
        _ => SparkBadgeTone.Neutral
    };

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