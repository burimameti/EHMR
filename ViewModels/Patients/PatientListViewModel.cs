using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;

namespace EHMR.ViewModels;

public partial class PatientListViewModel : BaseViewModel<Patient>
{
    private readonly IPatientService _patientService;
    private readonly ISelectedItemService<Patient> _selectedItemService;
    private readonly IAuthorizationService _authorization;

    // =========================
    // UI PERMISSIONS (MODULE BASED)
    // =========================
    [ObservableProperty] private bool canCreatePatients;

    [ObservableProperty] private bool canUpdatePatients;
    [ObservableProperty] private bool canDeletePatients;

    // filters
    private string _selectedStatus = "Сите";

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if(SetProperty(ref _selectedStatus, value))
                _=ApplyFilterAsync();
        }
    }

    private string _selectedGender = "Сите";

    public string SelectedGender
    {
        get => _selectedGender;
        set
        {
            if(SetProperty(ref _selectedGender, value))
                _=ApplyFilterAsync();
        }
    }

    public List<string> StatusFilters { get; } = ["Сите", "Active", "Inactive", "Critical"];
    public List<string> GenderFilters { get; } = ["Сите", "Male", "Female"];

    // =========================
    // CTOR
    // =========================
    public PatientListViewModel(
        IPatientService patientService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<Patient> selectedItemService,
        IAuthorizationService authorization)
        : base(navigationService, userDialogService, menuService, authorization)
    {
        _patientService=patientService;
        _selectedItemService=selectedItemService;
        _authorization=authorization;

        EvaluatePermissions();
    }

    // =========================
    // MODULE-BASED PERMISSIONS
    // =========================
    private void EvaluatePermissions()
    {
        var hasModule = _authorization.CanAccessModule("patients");

        CanCreatePatients=hasModule;
        CanUpdatePatients=hasModule;
        CanDeletePatients=hasModule;
    }

    // =========================
    // LIFECYCLE
    // =========================
    public override async Task OnAppearingAsync()
    {
        EvaluatePermissions();

        if(!_authorization.CanAccessModule("patients"))
        {
            await UserDialogService.ShowAlertAsync(
                "Пристап одбиен",
                "Немате пристап до пациенти.",
                "OK");

            await NavigationService.GoToAsync("//Dashboard");
            return;
        }

        await LoadAsync();
    }

    // =========================
    // LOAD
    // =========================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            var patients = await _patientService.GetAllAsync();

            _allItems=patients;
            await ApplyFilterAsync();
        }
        catch(Exception ex)
        {
            OnError($"Грешка: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================
    // FILTER
    // =========================
    protected override IEnumerable<Patient> FilterItems(string searchText, IEnumerable<Patient> items)
    {
        var query = items;

        if(!string.IsNullOrWhiteSpace(searchText))
        {
            query=query.Where(x =>
                x.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)==true||
                x.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)==true||
                x.Email?.Contains(searchText, StringComparison.OrdinalIgnoreCase)==true);
        }

        if(SelectedStatus!="Сите")
            query=query.Where(x => x.Status.ToString()==SelectedStatus);

        if(SelectedGender!="Сите")
            query=query.Where(x => x.Gender.ToString()==SelectedGender);

        return query;
    }

    // =========================
    // COMMANDS
    // =========================
    [RelayCommand]
    private async Task AddAsync()
    {
        if(!CanCreatePatients)
        {
            await UserDialogService.ShowAlertAsync("Одбиено", "Немате пристап.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(Patient patient)
    {
        _selectedItemService.SelectedItem=patient;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Patient patient)
    {
        if(!CanUpdatePatients)
        {
            await UserDialogService.ShowAlertAsync("Одбиено", "Немате пристап.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=patient;
        await NavigationService.GoToAsync(AppRoutes.Patients.Detail);
    }

    [RelayCommand]
    private async Task DeleteAsync(Patient patient)
    {
        if(!CanDeletePatients)
        {
            await UserDialogService.ShowAlertAsync("Одбиено", "Немате пристап.", "OK");
            return;
        }

        var confirm = await UserDialogService.ShowConfirmationAsync(
            "Бришење",
            $"Избриши {patient.FirstName} {patient.LastName}?");

        if(!confirm) return;

        try
        {
            IsBusy=true;

            await _patientService.DeleteAsync(patient.Id);

            _allItems.Remove(patient);
            await ApplyFilterAsync();
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText=string.Empty;
        SelectedStatus="Сите";
        SelectedGender="Сите";

        await ApplyFilterAsync();
    }
}