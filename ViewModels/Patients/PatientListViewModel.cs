using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.UI.Lookup;
using EHMR.ViewModels.Patients.Extensions;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Patients;

public partial class PatientListViewModel : BaseViewModel<Patient>, IQueryAttributable
{
    private string? _pendingSearch;
    private string? _pendingStatus;

    private readonly FilterLookup _cityLookup = PatientFilterLookups.BuildCityLookup();

    private readonly IPatientService _patientService;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty] private bool canCreatePatients;
    [ObservableProperty] private bool canUpdatePatients;
    [ObservableProperty] private bool canDeletePatients;

    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedGender = "All";
    [ObservableProperty] private string selectedBloodType = "All";
    [ObservableProperty] private string selectedCity = "All";
    [ObservableProperty] private string selectedAgeGroup = "All";

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
            if(SelectedStatus==internalValue)
                return;

            SelectedStatus=internalValue;
            CurrentPage=1;
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
            if(SelectedGender==internalValue)
                return;

            SelectedGender=internalValue;
            CurrentPage=1;
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
            if(SelectedBloodType==internalValue)
                return;

            SelectedBloodType=internalValue;
            CurrentPage=1;
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
            if(SelectedCity==internalValue)
                return;

            SelectedCity=internalValue;
            CurrentPage=1;
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
            if(SelectedAgeGroup==internalValue)
                return;

            SelectedAgeGroup=internalValue;
            CurrentPage=1;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    private readonly ISelectedItemService<Patient> _selectedItemService;
    public PatientListViewModel(
        IPatientService patientService, ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authorization)
        : base(navigationService, userDialogService, menuService, authorization)
    {
        _patientService=patientService;
        _authorization=authorization;
        _selectedItemService=selectedItemService;
        CityFilterNames=_cityLookup.ToObservableCollection();
        EvaluatePermissions();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
    }
    

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            var patients = await _patientService.GetAllAsync();
            AllItems=patients?.ToList()??new List<Patient>();
            ApplyPendingQuery();
            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Failed to load patients: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void ApplyPendingQuery()
    {
        if(!string.IsNullOrWhiteSpace(_pendingSearch))
            SearchText=_pendingSearch;

        if(!string.IsNullOrWhiteSpace(_pendingStatus))
            SelectedStatus=_pendingStatus;

        CurrentPage=1;
        _pendingSearch=null;
        _pendingStatus=null;
    }

    [RelayCommand]
    private void SelectedStatusChanged(string? value)
    {
        SelectedStatus=PatientFilterLookups.Status.ToInternal(value);
        CurrentPage=1;
        ApplyPipeline();
    }

    [RelayCommand]
    private void SelectedGenderChanged(string? value)
    {
        SelectedGender=PatientFilterLookups.Gender.ToInternal(value);
        CurrentPage=1;
        ApplyPipeline();
    }

    [RelayCommand]
    private void SelectedBloodTypeChanged(string? value)
    {
        SelectedBloodType=PatientFilterLookups.BloodType.ToInternal(value);
        CurrentPage=1;
        ApplyPipeline();
    }

    [RelayCommand]
    private void SelectedCityChanged(string? value)
    {
        SelectedCity=_cityLookup.ToInternal(value);
        CurrentPage=1;
        ApplyPipeline();
    }

    [RelayCommand]
    private void SelectedAgeGroupChanged(string? value)
    {
        SelectedAgeGroup=PatientFilterLookups.AgeGroup.ToInternal(value);
        CurrentPage=1;
        ApplyPipeline();
    }

    [RelayCommand]
    private async Task ClearFilters()
    {
        SearchText=string.Empty;

        SelectedStatus="All";
        SelectedGender="All";
        SelectedBloodType="All";
        SelectedCity="All";
        SelectedAgeGroup="All";

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedGenderDisplay));
        OnPropertyChanged(nameof(SelectedBloodTypeDisplay));
        OnPropertyChanged(nameof(SelectedCityDisplay));
        OnPropertyChanged(nameof(SelectedAgeGroupDisplay));

        CurrentPage=1;
        ApplyPipeline();
        
    }

    [RelayCommand]
    private async Task AddAsync() => await NavigationService.GoToAsync(AppRoutes.Patients.Detail);

    [RelayCommand]
    private async Task SelectAsync(Patient? patient)
    {
        if(patient==null)
            return;

        _selectedItemService.SelectedItem=patient;
        _selectedItemService.OpenInEditMode=false;

        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Patient? patient)
    {
        if(patient==null)
            return;

        _selectedItemService.SelectedItem=patient;
        _selectedItemService.OpenInEditMode=true;

        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }
    [RelayCommand]
    private async Task ViewHistoryAsync(Patient? patient)
    {
        if(patient==null) return;
    }

    protected override IEnumerable<Patient> ApplyFilters(IEnumerable<Patient> query)
    {
        if(SelectedStatus!="All")
            query=query.Where(x => string.Equals(x.Status.ToString(), SelectedStatus, StringComparison.OrdinalIgnoreCase));

        if(SelectedGender!="All")
            query=query.Where(x => string.Equals(x.Gender.ToString(), SelectedGender, StringComparison.OrdinalIgnoreCase));

        if(SelectedBloodType!="All")
            query=query.Where(x => string.Equals(x.BloodType, SelectedBloodType, StringComparison.OrdinalIgnoreCase));

        if(SelectedCity!="All")
            query=query.Where(x => string.Equals(x.City, SelectedCity, StringComparison.OrdinalIgnoreCase));

        if(SelectedAgeGroup!="All")
            query=query.Where(x => x.Age.IsInAgeGroup(SelectedAgeGroup));

        return query;
    }

    protected override IEnumerable<Patient> ApplySearch(IEnumerable<Patient> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return query;

        search=search.Trim().ToLowerInvariant();

        return query.Where(x =>
            (!string.IsNullOrWhiteSpace(x.FullName)&&x.FullName.ToLowerInvariant().Contains(search))||
            (!string.IsNullOrWhiteSpace(x.NationalId)&&x.NationalId.Contains(search, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.Phone)&&x.Phone.Contains(search, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.City)&&x.City.ToLowerInvariant().Contains(search)));
    }

    private void EvaluatePermissions()
    {
        var hasModule = _authorization.CanAccessModule("patients");
        CanCreatePatients=hasModule;
        CanUpdatePatients=hasModule;
        CanDeletePatients=hasModule;
    }
}