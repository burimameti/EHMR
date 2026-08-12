using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;


namespace EHMR.ViewModels.Appointments;

public partial class AppointmentDetailViewModel : BaseDetailViewModel<Appointment>
{
    private readonly IAppointmentDetailService _service;
    private readonly ISelectedItemService<Appointment> _selectedItemService;

    private CancellationTokenSource _cts = new();

    private Appointment? _originalAppointment;
    private bool _isNewAppointmentMode;
    private bool _isModalReturnMode;
    private bool _isApplyingAutomaticSlot;
    private bool _suppressPatientSelection;
    private int _slotRequestVersion;

    protected override string ModuleName => Modules.Appointments;

    public bool IsNewAppointment => _isNewAppointmentMode;
    public bool CanSaveAppointment => IsNewAppointment ? CanCreate : CanUpdate;
    public bool CanEditAppointment => !_isNewAppointmentMode&&CanUpdate&&
        Appointment.Status is (AppointmentStatus.Scheduled or AppointmentStatus.CheckedIn)&&
        Appointment.ScheduledStart.Date>=DateTime.Today;
    public bool ShowStatusEditor => IsEditMode&&!IsNewAppointment;

    public string HeaderTitle =>
        _isNewAppointmentMode
            ? "Нов Термин"
            : $"Термин: {SelectedPatientForAppointment?.FullName}";

    public string HeaderSubtitle =>
        _isNewAppointmentMode
            ? "Закажување нов термин"
            : $"Реуматолог: {SelectedDoctorForAppointment?.FullName} • {Appointment.ScheduledStart:dd.MM.yyyy HH:mm}";

    public AppointmentDetailViewModel(
        IAuthorizationService authorizationService,
        IAppointmentDetailService service,
        INavigationService navigation,
        ISelectedItemService<Appointment> selected,
        IUserDialogService userDialogService, ISelectedItemService<Appointment> selectedItemService,
        IMenuService menuService)
        : base(navigation, userDialogService, menuService, authorizationService,selectedItemService)
    {
        _service=service;
        _selectedItemService=selected;
        EvaluatePermissions(); // now sets base's CanCreate/CanUpdate/CanDelete
    }

    // =========================
    // MAIN DATA
    // =========================

    [ObservableProperty] private Appointment appointment = new();

    [ObservableProperty] private ObservableCollection<Diagnosis> selectedDiagnoses = new();
    [ObservableProperty] private ObservableCollection<TherapyCycle> visibleTherapies = new();

    [ObservableProperty] private ObservableCollection<Patient> patientsList = new();
    [ObservableProperty] private ObservableCollection<Doctor> doctorsList = new();

    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
    [ObservableProperty] private ObservableCollection<Diagnosis> patientDiagnosisHistory = new();
    [ObservableProperty] private ObservableCollection<PatientMedicine> patientMedicinesHistory = new();
    [ObservableProperty] private ObservableCollection<Mkb10Code> availableDiagnoses = new();

    // =========================
    // UI STATE
    // =========================

    [ObservableProperty] private string pageTitle = "Детали за термин";

    [ObservableProperty] private bool showDiagnosisDropdown;
    [ObservableProperty] private string diagnosisSearchText = string.Empty;

    // =========================
    // TIME
    // =========================

    [ObservableProperty] private TimeSpan selectedStartTime;
    [ObservableProperty] private TimeSpan selectedEndTime;
    [ObservableProperty] private DateTime preferredAppointmentDate = DateTime.Today;
    [ObservableProperty] private string selectedSchedulingHorizon = "Следен слободен термин";

    public IReadOnlyList<string> SchedulingHorizonOptions { get; } =
        ["Следен слободен термин", "За 1 недела", "За 2 недели", "За 3 недели", "За 1 месец"];

    public string AutomaticSlotText => SelectedPatientForAppointment is null
        ? "Изберете пациент за автоматски термин"
        : SelectedDoctorForAppointment is null
        ? "Пациентот нема доделен матичен реуматолог"
        : $"Прв слободен термин: {Appointment.ScheduledStart:dd.MM.yyyy HH:mm}";

    // =========================
    // SELECTIONS
    // =========================

    [ObservableProperty] private Patient? selectedPatientForAppointment;
    [ObservableProperty] private Doctor? selectedDoctorForAppointment;
    [ObservableProperty] private TherapyCycle? selectedTherapyCycle;

    public IReadOnlyList<AppointmentStatusChoice> StatusOptions { get; } =
    [
        new(AppointmentStatus.Scheduled, "Закажан"),
        new(AppointmentStatus.CheckedIn, "Пријавен"),
        new(AppointmentStatus.InProgress, "Во тек"),
        new(AppointmentStatus.Completed, "Завршен"),
        new(AppointmentStatus.Cancelled, "Откажан"),
        new(AppointmentStatus.Missed, "Пропуштен"),
        new(AppointmentStatus.ReScheduled, "Презакажан")
    ];

    public AppointmentStatusChoice? SelectedStatusOption
    {
        get => StatusOptions.FirstOrDefault(x => x.Value==Appointment.Status);
        set
        {
            if(value==null||Appointment.Status==value.Value) return;
            Appointment.Status=value.Value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Appointment));
            OnPropertyChanged(nameof(CanEditAppointment));
        }
    }

partial void OnSelectedDoctorForAppointmentChanged(Doctor? value)
    {
        if(value!=null&&IsNewAppointment)
            _=AssignNextAvailableSlotAsync();
        OnPropertyChanged(nameof(AutomaticSlotText));
    }

    partial void OnSelectedPatientForAppointmentChanged(Patient? value)
    {
        if(!_suppressPatientSelection&&value!=null&&IsNewAppointment)
            _=OnPatientChangedAsync(value);

        OnPropertyChanged(nameof(AutomaticSlotText));
    }

    partial void OnPreferredAppointmentDateChanged(DateTime value)
    {
        if(!_isApplyingAutomaticSlot&&SelectedDoctorForAppointment!=null&&IsNewAppointment)
            _=AssignNextAvailableSlotAsync();
    }

    partial void OnSelectedSchedulingHorizonChanged(string value)
    {
        if(!IsNewAppointment)
            return;

        var date=value switch
        {
            "За 1 недела" => DateTime.Today.AddDays(7),
            "За 2 недели" => DateTime.Today.AddDays(14),
            "За 3 недели" => DateTime.Today.AddDays(21),
            "За 1 месец" => DateTime.Today.AddMonths(1),
            _ => DateTime.Today
        };

        PreferredAppointmentDate=date;
        if(SelectedDoctorForAppointment!=null)
            _=AssignNextAvailableSlotAsync();
    }

    private async Task AssignNextAvailableSlotAsync()
    {
        if(SelectedPatientForAppointment is null||SelectedDoctorForAppointment is null)
            return;

        var requestVersion=++_slotRequestVersion;
        var from=PreferredAppointmentDate.Date==DateTime.Today
            ? DateTime.Now
            : PreferredAppointmentDate.Date.AddHours(8).AddMinutes(30);
        var slot=await _service.GetNextAvailableSlot(
            SelectedDoctorForAppointment.Id,
            SelectedPatientForAppointment.Id,
            from,
            30);
        if(requestVersion!=_slotRequestVersion)
            return;

        _isApplyingAutomaticSlot=true;
        try
        {
            Appointment.ScheduledStart=slot;
            Appointment.ScheduledEnd=slot.AddMinutes(30);
            PreferredAppointmentDate=slot.Date;
            SelectedStartTime=slot.TimeOfDay;
            SelectedEndTime=slot.AddMinutes(30).TimeOfDay;
            OnPropertyChanged(nameof(Appointment));
            OnPropertyChanged(nameof(AutomaticSlotText));
        }
        finally
        {
            _isApplyingAutomaticSlot=false;
        }
    }

    // =========================
    // LOAD  (unchanged logic, only IsReadOnly comes from base now)
    // =========================

    public async Task LoadAsync()
    {
        var selected = _selectedItemService.SelectedItem;

        Guid? prefillPatientId = null;
        if(selected is { Id: var sid }&&sid==Guid.Empty&&selected.PatientId!=Guid.Empty)
        {
            prefillPatientId=selected.PatientId;
            _isModalReturnMode=true;
            selected=null;
        }

        if(selected==null)
        {
            await LoadNewAppointmentAsync(prefillPatientId);
            return;
        }

        _isNewAppointmentMode=false;

        var id = selected.Id;
        if(id==Guid.Empty) return;

        var dto = await _service.GetAppointment(id);
        var ctx = await _service.GetPatientContext(dto.Appointment.PatientId);

        _originalAppointment=dto.Appointment.Clone();
        Appointment=dto.Appointment;

        SelectedDiagnoses=new ObservableCollection<Diagnosis>(dto.Diagnoses);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);

        PatientsList=new ObservableCollection<Patient>(dto.Patients);
        DoctorsList=new ObservableCollection<Doctor>(dto.Doctors);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientDiagnosisHistory=new ObservableCollection<Diagnosis>(ctx.Diagnoses);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);

        SelectedPatientForAppointment=PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);
        SelectedTherapyCycle=VisibleTherapies.FirstOrDefault(x => x.Id==Appointment.TherapyCycleId);

        PreferredAppointmentDate=Appointment.ScheduledStart.Date;
        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;

        PageTitle=$"Термин: {SelectedPatientForAppointment?.FullName}";
        IsReadOnly=!_selectedItemService.OpenInEditMode;
        EvaluatePermissions();

        OnPropertyChanged(nameof(IsNewAppointment));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    private async Task LoadNewAppointmentAsync(Guid? prefillPatientId)
    {
        _isNewAppointmentMode=true;
        _originalAppointment=null;

        Appointment=CreateBlankAppointment();

        var lookups = await _service.GetAppointmentContext();
        PatientsList=new ObservableCollection<Patient>(lookups.Patients);
        DoctorsList=new ObservableCollection<Doctor>(lookups.Doctors);

        _suppressPatientSelection=true;
        SelectedPatientForAppointment=prefillPatientId is { } pid
            ? PatientsList.FirstOrDefault(x => x.Id==pid)
            : null;
        _suppressPatientSelection=false;

        Appointment.PatientId=SelectedPatientForAppointment?.Id??Guid.Empty;

        AppointmentHistory=new ObservableCollection<Appointment>();
        PatientDiagnosisHistory=new ObservableCollection<Diagnosis>();
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>();
        VisibleTherapies=new ObservableCollection<TherapyCycle>();
        SelectedTherapyCycle=null;
        SelectedDiagnoses=new ObservableCollection<Diagnosis>();

        PreferredAppointmentDate=Appointment.ScheduledStart.Date;
        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;

        PageTitle="Нов Термин";
        IsReadOnly=false;

        if(SelectedPatientForAppointment!=null)
            await OnPatientChangedAsync(SelectedPatientForAppointment);
        else
            SelectedDoctorForAppointment=null;

        OnPropertyChanged(nameof(IsNewAppointment));
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    private static Appointment CreateBlankAppointment()
    {
        var start = DateTime.Today.AddHours(9);
        return new Appointment
        {
            Id=Guid.Empty,
            ScheduledStart=start,
            ScheduledEnd=start.AddMinutes(30),
            Status=AppointmentStatus.Scheduled
        };
    }

    // =========================
    // PATIENT CHANGE
    // =========================

    public async Task OnPatientChangedAsync(Patient patient)
    {
        if(patient==null) return;

        Appointment.PatientId=patient.Id;
        SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==patient.DoctorId);
        Appointment.DoctorId=SelectedDoctorForAppointment?.Id??Guid.Empty;

        var ctx = await _service.GetPatientContext(patient.Id);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.Appointments);
        PatientDiagnosisHistory=new ObservableCollection<Diagnosis>(ctx.Diagnoses);
        PatientMedicinesHistory=new ObservableCollection<PatientMedicine>(ctx.PatientMedicines);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);

        SelectedTherapyCycle=VisibleTherapies.FirstOrDefault();

        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(AutomaticSlotText));
    }

    [RelayCommand]
    private Task PatientChanged(Patient patient) => OnPatientChangedAsync(patient);

    // =========================
    // DIAGNOSIS SEARCH  (unchanged)
    // =========================

    partial void OnDiagnosisSearchTextChanged(string value) =>
        _=SearchDiagnosesAsync(value);

    [RelayCommand]
    private async Task SearchDiagnosesAsync(string query)
    {
        if(string.IsNullOrWhiteSpace(query))
        {
            AvailableDiagnoses.Clear();
            ShowDiagnosisDropdown=false;
            return;
        }

        try
        {
            _cts.Cancel();
            _cts=new CancellationTokenSource();

            var result = await _service.SearchDiagnoses(query, _cts.Token);

            AvailableDiagnoses=new ObservableCollection<Mkb10Code>(result);
            ShowDiagnosisDropdown=AvailableDiagnoses.Count>0;
        }
        catch(TaskCanceledException) { }
    }

    [RelayCommand]
    private void AddDiagnosis(Mkb10Code code)
    {
        if(code==null) return;
        if(SelectedDiagnoses.Any(x => x.Mkb10CodeId==code.Id)) return;

        SelectedDiagnoses.Add(new Diagnosis
        {
            Mkb10CodeId=code.Id,
            Id=code.Id,
            Mkb10Code=code,
            IsPrimary=false
        });

        DiagnosisSearchText=string.Empty;
        AvailableDiagnoses.Clear();
        ShowDiagnosisDropdown=false;
    }

    [RelayCommand]
    private void RemoveDiagnosis(Diagnosis d)
    {
        if(d!=null) SelectedDiagnoses.Remove(d);
    }

    // =========================
    // COMMANDS  (toggle edit now overrides base hook)
    // =========================

    [RelayCommand]
    private async Task NewAppointmentForPatient(Patient patient)
    {
        if(patient is null) return;
        if(!CanCreate)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање термини.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=new Appointment { Id=Guid.Empty, PatientId=patient.Id };
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private void ToggleEditMode()
    {
        ToggleEdit();
        OnPropertyChanged(nameof(ShowStatusEditor));
    }

   

    // =========================
    // SAVE  (unchanged)
    // =========================
    [RelayCommand]
    private async Task SaveAsync()
    {
        if(SelectedPatientForAppointment==null)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Изберете пациент.", "OK");
            return;
        }

        if(SelectedDoctorForAppointment==null)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Избраниот пациент нема доделен матичен реуматолог.", "OK");
            return;
        }

        if(IsNewAppointment)
            await AssignNextAvailableSlotAsync();

        if(Appointment.ScheduledEnd<=Appointment.ScheduledStart)
        {
            await UserDialogService.ShowAlertAsync("Валидација", "Не може да се одреди валиден слободен термин.", "OK");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            Appointment.PatientId=SelectedPatientForAppointment.Id;
            Appointment.DoctorId=SelectedDoctorForAppointment.Id;
            Appointment.TherapyCycleId=SelectedTherapyCycle?.Id;

            //if(_isNewAppointmentMode)
            //{
            //    Appointment.Id=Guid.NewGuid();
            //    Appointment.CreatedAt=DateTime.UtcNow;
            //}

            await _service.SaveAppointment(Appointment, SelectedDiagnoses.ToList(), SelectedTherapyCycle);
            await UserDialogService.ShowAlertAsync("Успешно", "Терминот е успешно зачуван.", "OK");

            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=Appointment;
                await NavigationService.GoToAsync("..");
            }
            else if(_isNewAppointmentMode)
            {
                _selectedItemService.SelectedItem=null;
                await NavigationService.GoToAsync(AppRoutes.Appointments.List);
            }
            else
            {
                _originalAppointment=Appointment.Clone();
                IsReadOnly=true;
            }
        }, "Грешка при зачувување на терминот");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await ExecuteSafeAsync(async () =>
        {
            if(_isModalReturnMode)
            {
                _selectedItemService.SelectedItem=null;
                await NavigationService.GoToAsync("..");
                return;
            }

            if(_isNewAppointmentMode)
            {
                await NavigationService.GoToAsync(AppRoutes.Appointments.List);
                return;
            }

            if(_originalAppointment!=null)
            {
                Appointment=_originalAppointment.Clone();
                SelectedPatientForAppointment=PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);
                SelectedDoctorForAppointment=DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);
                SelectedTherapyCycle=VisibleTherapies.FirstOrDefault(x => x.Id==Appointment.TherapyCycleId);
                SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
                SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;
            }

            IsReadOnly=true;
            await Task.CompletedTask;
        }, "Грешка при откажување");
    }

    [RelayCommand]
    private async Task SelectAppointmentAsync(Appointment appointment)
    {
        if(appointment==null) return;
        _selectedItemService.SelectedItem=appointment;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GenerateNextCycleAsync()
    {
        await _service.GenerateNextTherapyCycle(Appointment);
        await LoadAsync();
    }
}

public static class AppointmentExtensions
{
    public static Appointment Clone(this Appointment source)
    {
        if(source is null) return new Appointment();
        return new Appointment
        {
            Id=source.Id,
            AppointmentNumber=source.AppointmentNumber,
            PatientId=source.PatientId,
            DoctorId=source.DoctorId,
            TherapyCycleId=source.TherapyCycleId,
            ScheduledStart=source.ScheduledStart,
            ScheduledEnd=source.ScheduledEnd,
            Status=source.Status,
            ReasonForVisit=source.ReasonForVisit,
            ClinicalNotes=source.ClinicalNotes,
            CreatedAt=source.CreatedAt,
            Patient=source.Patient,
            Doctor=source.Doctor,
            TherapyCycle=source.TherapyCycle
        };
    }
}
public sealed record AppointmentStatusChoice(AppointmentStatus Value, string Label)
{
    public override string ToString() => Label;
}
