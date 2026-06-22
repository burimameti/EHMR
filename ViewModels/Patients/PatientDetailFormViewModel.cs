using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
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
    private bool _isModalReturnMode; // Флаг кој кажува дека сме дојдени од друга форма (пр. Термин)

    [ObservableProperty] private Patient patient = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    [NotifyPropertyChangedFor(nameof(InputBgColor))]
    [NotifyPropertyChangedFor(nameof(InputBorderColor))]
    private bool isReadOnly = true;

    [ObservableProperty] private string pageTitle = string.Empty;

    public bool IsEditMode => !IsReadOnly;
    public string InputBgColor => IsReadOnly ? "#F8FAFC" : "#FFFFFF";
    public string InputBorderColor => IsReadOnly ? "#E2E8F0" : "#2563EB";

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

        // Паметна проверка: Ако објектот има специфичен Id или маркер, знаеме дека доаѓаме од Appointment екранот
        if(selectedPatient!=null&&selectedPatient.Id==Guid.Empty&&selectedPatient.FirstName=="APPOINTMENT_CONTEXT")
        {
            _isModalReturnMode=true;
            selectedPatient=null; // Ресетирај за да се креира чиста инстанца
        }

        if(selectedPatient==null)
        {
            _isNewPatientMode=true;
            Patient=new Patient
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

            PageTitle="➕ Креирај Нов Пациент за Термин";
            IsReadOnly=false;
            return;
        }

        _isNewPatientMode=false;
        _originalPatient=selectedPatient;
        Patient=ClonePatient(selectedPatient);
        PageTitle=$"Досие: {Patient.FirstName} {Patient.LastName}";
        IsReadOnly=true;
    }

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

                // Мапирање на измените...
                existing.FirstName=Patient.FirstName;
                existing.LastName=Patient.LastName;
                existing.NationalId=Patient.NationalId;
                existing.BirthDate=Patient.BirthDate;
                existing.Gender=Patient.Gender;
                existing.Phone=Patient.Phone;
                existing.Email=Patient.Email;
                existing.Status=Patient.Status;
            }

            await db.SaveChangesAsync();

            await _userDialogService.ShowAlertAsync("Успешно", "Пациентот е успешно зачуван.", "OK");

            if(_isModalReturnMode)
            {
                // КЛУЧОТ: Го оставаме новиот пациент во споделениот сервис за Appointment да го прочита
                _selectedItemService.SelectedItem=Patient;
                await _navigationService.GoToAsync(".."); // Се враќа назад кон формата за термин
            }
            else
            {
                _selectedItemService.SelectedItem=null;
                await _navigationService.GoToAsync(AppRoutes.Patients.List);
            }
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", ex.Message, "OK");
        }
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

        if(_originalPatient!=null) Patient=ClonePatient(_originalPatient);
        IsReadOnly=true;
    }

    private static Patient ClonePatient(Patient source)
    {
        return new Patient
        {
            Id=source.Id,
            FirstName=source.FirstName,
            LastName=source.LastName,
            NationalId=source.NationalId,
            BirthDate=source.BirthDate,
            Gender=source.Gender,
            Phone=source.Phone,
            Email=source.Email,
            Address=source.Address,
            City=source.City,
            Status=source.Status
        };
    }
}