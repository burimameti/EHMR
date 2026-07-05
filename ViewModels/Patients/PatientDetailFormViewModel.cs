using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Patients.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class PatientDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Patient> _selectedItemService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _userDialogService;

    private Patient? _originalPatient;
    private bool _isNewPatientMode;
    private bool _isModalReturnMode;
    public bool IsNewPatient => _isNewPatientMode;

    public string HeaderTitle =>
        _isNewPatientMode
            ? "Нов Пациент"
            : $" Детали за пациент : Име {Patient.FirstName} Презиме: {Patient.LastName}";

    public string HeaderSubtitle =>
        _isNewPatientMode
            ? "Креирање ново пациентско досие"
            : $"Матичен: {Patient.NationalId} • Возраст {Patient.Age} год.";

    [ObservableProperty] private Patient patient = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(InputBgColor))]
    [NotifyPropertyChangedFor(nameof(InputBorderColor))]
    private bool isReadOnly = true;

    [ObservableProperty] private string pageTitle = string.Empty;


    public bool IsEditMode => !IsReadOnly;
    public Color InputBgColor =>
        IsReadOnly
        ? Color.FromArgb("#F8FAFC")
        : Color.FromArgb("#FFFFFF");


    public Color InputBorderColor =>
        IsReadOnly
        ? Color.FromArgb("#CBD5E1")
        : Color.FromArgb("#2563EB");

    public ObservableCollection<string> GenderOptions { get; } = PatientEnumLookups.Gender.ToObservableCollection();
    public ObservableCollection<string> StatusOptions { get; } = PatientEnumLookups.Status.ToObservableCollection();
    public ObservableCollection<string> BloodTypeOptions { get; } = PatientFilterLookups.BloodType.ToObservableCollection();

    public ObservableCollection<string> CityOptions
    {
        get;
    } =
       new(PatientFilterLookups.BuildCityLookup().DisplayValues); // skip "Сите"

    // Doctor: се полни при InitializeForm од база
    public ObservableCollection<string> DoctorOptions { get; } = new();

    // Relation
    public ObservableCollection<string> RelationOptions
    {
        get;
    } = new()
    {
        "Сопруг / Сопруга",
        "Родител",
        "Дете",
        "Брат / Сестра",
        "Пријател",
        "Друго"
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTab2Active))]
    [NotifyPropertyChangedFor(nameof(IsTab3Active))]
    [NotifyPropertyChangedFor(nameof(IsTab4Active))]
    private int activeTab = 1;

    public bool IsTab1Active => ActiveTab==1;
    public bool IsTab2Active => ActiveTab==2;
    public bool IsTab3Active => ActiveTab==3;
    public bool IsTab4Active => ActiveTab==4;

    [ObservableProperty] private string selectedGenderDisplay = string.Empty;
    [ObservableProperty] private string selectedStatusDisplay = string.Empty;
    [ObservableProperty] private string selectedDoctorDisplay = string.Empty;
    [ObservableProperty] private string selectedRelationDisplay = string.Empty;
    // Needed because ActiveTab notifies only Tab2/3/4
    partial void OnActiveTabChanged(int value) =>
        OnPropertyChanged(nameof(IsTab1Active));

    [RelayCommand]
    private void SelectTab(string tab)
    {
        if(int.TryParse(tab, out var t))
            ActiveTab=t;
    }

    public PatientDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Patient> selectedItemService,
        INavigationService navigationService,
        IUserDialogService userDialogService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;

        InitializeForm();
    }

    private void InitializeForm()
    {
        var selectedPatient = _selectedItemService.SelectedItem;

        if(selectedPatient is { Id: var id, FirstName: "APPOINTMENT_CONTEXT" }&&id==Guid.Empty)
        {
            _isModalReturnMode=true;
            selectedPatient=null;
        }

        if(selectedPatient==null)
        {
            _isNewPatientMode=true;
            Patient=CreateBlankForRegistration();
            PageTitle="Нов Пациент";
            IsReadOnly=false;

            OnPropertyChanged(nameof(IsNewPatient));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(HeaderSubtitle));
            return;
        }

        _originalPatient=selectedPatient;
        Patient=selectedPatient.Clone();

        PageTitle=$"Досие: {Patient.FullName}";

        IsReadOnly=!_selectedItemService.OpenInEditMode;

        OnPropertyChanged(nameof(IsNewPatient));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    // ------------------------------------------------------------------ //
    // Commands
    // ------------------------------------------------------------------ //

    [RelayCommand]
    private void ToggleEditMode() => IsReadOnly=!IsReadOnly;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(Patient.FirstName)||string.IsNullOrWhiteSpace(Patient.LastName))
        {
            await _userDialogService.ShowAlertAsync("Валидација", "Името и презимето се задолжителни.", "OK");
            return;
        }

        if(string.IsNullOrWhiteSpace(Patient.NationalId)||Patient.NationalId.Length!=13)
        {
            await _userDialogService.ShowAlertAsync("Валидација", "ЕМБГ мора да содржи точно 13 цифри.", "OK");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            if(_isNewPatientMode)
            {
                Patient.Id=Guid.NewGuid();
                Patient.RegistrationDate=DateTime.UtcNow;
                Patient.CreatedAt=DateTime.UtcNow;
                await db.Patients.AddAsync(Patient);
            }
            else
            {
                var existing = await db.Patients.FirstOrDefaultAsync(x => x.Id==Patient.Id);
                if(existing==null) return;

                existing.FirstName=Patient.FirstName;
                existing.LastName=Patient.LastName;
                existing.NationalId=Patient.NationalId;
                existing.SSN=Patient.SSN;
                existing.BirthDate=Patient.BirthDate;
                existing.Gender=Patient.Gender;
                existing.Phone=Patient.Phone;
                existing.Email=Patient.Email;
                existing.Address=Patient.Address;
                existing.City=Patient.City;
                existing.Status=Patient.Status;
                existing.BloodType=Patient.BloodType;
                existing.Allergies=Patient.Allergies;
                existing.EmergencyContactName=Patient.EmergencyContactName;
                existing.EmergencyContactPhone=Patient.EmergencyContactPhone;
                 db.Patients.Update(existing);
            }

            await db.SaveChangesAsync();
            await _userDialogService.ShowAlertAsync("Успешно", "Пациентот е успешно зачуван.", "OK");

            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=Patient;
                await _navigationService.GoToAsync("..");
            }
            else
            {
                _selectedItemService.SelectedItem=null;
                await _navigationService.GoToAsync(AppRoutes.Patients.List);
            }
            _selectedItemService.OpenInEditMode=false;
            _selectedItemService.SelectedItem=null;
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", ex.Message, "OK");
        }
    }
    private void SyncDisplayFromPatient()
    {
        SelectedGenderDisplay=PatientEnumLookups.Gender.ToDisplay(Patient.Gender.ToString());
        SelectedStatusDisplay=PatientEnumLookups.Status.ToDisplay(Patient.Status.ToString());
    }

    private async Task LoadDoctorsAsync()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var doctors = await db.Users
                .Where(u => u.Role==UserRole.Doctor)
                .Select(u => $"Д-р {u.FirstName} {u.LastName}")
                .ToListAsync();

            DoctorOptions.Clear();
            DoctorOptions.Add("— Не е доделен —");
            foreach(var d in doctors)
                DoctorOptions.Add(d);
        }
        catch { /* non-critical */ }
    }
    [RelayCommand]
    private async Task CancelAsync()
    {
        if(_isModalReturnMode)
        {
            _selectedItemService.SelectedItem=null;
            await _navigationService.GoToAsync(AppRoutes.Patients.List);
            return;
        }

        if(_isNewPatientMode)
        {
            await _navigationService.GoToAsync(AppRoutes.Patients.List);
            return;
        }

        if(_originalPatient!=null) Patient=_originalPatient.Clone();
        IsReadOnly=true;
    }


    public static Patient CreateBlankForRegistration() => new()
    {
        FirstName=string.Empty,
        LastName=string.Empty,
        NationalId=string.Empty,
        SSN=string.Empty,
        BirthDate=DateTime.Today.AddYears(-30),
        Gender=Gender.Male,
        Phone=string.Empty,
        Email=string.Empty,
        Address=string.Empty,
        City=string.Empty,
        EmergencyContactName=string.Empty,
        EmergencyContactPhone=string.Empty,
        BloodType=string.Empty,
        Allergies=string.Empty,
        RegistrationDate=DateTime.UtcNow,
        Status=PatientStatus.Active
    };
}