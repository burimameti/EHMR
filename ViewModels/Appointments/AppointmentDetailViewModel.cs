using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Appointments;

public partial class AppointmentDetailViewModel : ObservableObject
{
    private readonly IAppointmentDetailService _service;
    private readonly INavigationService _navigation;
    private readonly ISelectedItemService<Appointment> _selected;

    private CancellationTokenSource _cts = new();

    public AppointmentDetailViewModel(
        IAppointmentDetailService service,
        INavigationService navigation,
        ISelectedItemService<Appointment> selected)
    {
        _service=service;
        _navigation=navigation;
        _selected=selected;
    }

    // =========================
    // MAIN DATA
    // =========================

    [ObservableProperty] private Appointment appointment = new();

    [ObservableProperty] private ObservableCollection<AppointmentDiagnosis> selectedDiagnoses = new();
    [ObservableProperty] private ObservableCollection<TherapyCycle> visibleTherapies = new();

    [ObservableProperty] private ObservableCollection<Patient> patientsList = new();
    [ObservableProperty] private ObservableCollection<Doctor> doctorsList = new();

    [ObservableProperty] private ObservableCollection<Appointment> appointmentHistory = new();
    [ObservableProperty] private ObservableCollection<DiagnosisHistoryItem> patientDiagnosisHistory = new();

    [ObservableProperty] private ObservableCollection<Mkb10Code> availableDiagnoses = new();

    // =========================
    // UI STATE
    // =========================

    [ObservableProperty] private string pageTitle = "Детали за термин";
    [ObservableProperty] private bool isReadOnly = true;
    [ObservableProperty] private bool isEditMode;
    [ObservableProperty] private bool showDiagnosisDropdown;
    [ObservableProperty] private string diagnosisSearchText = string.Empty;

    // =========================
    // TIME
    // =========================

    [ObservableProperty] private TimeSpan selectedStartTime;
    [ObservableProperty] private TimeSpan selectedEndTime;

    // =========================
    // SELECTIONS (IMPORTANT FIX)
    // =========================

    [ObservableProperty] private Patient selectedPatientForAppointment;
    [ObservableProperty] private Doctor selectedDoctorForAppointment;
    [ObservableProperty] private TherapyCycle selectedTherapyCycle;

    // =========================
    // STATIC
    // =========================

    public List<string> StatusOptions =>
        Enum.GetNames(typeof(AppointmentStatus)).ToList();

    // =========================
    // LOAD
    // =========================

    public async Task LoadAsync()
    {
        var id = _selected.SelectedItem?.Id??Guid.Empty;
        if(id==Guid.Empty) return;

        var dto = await _service.GetAppointment(id);
        var ctx = await _service.GetPatientContext(dto.Appointment.PatientId);

        Appointment=dto.Appointment;

        SelectedDiagnoses=new ObservableCollection<AppointmentDiagnosis>(dto.Diagnoses);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);

        PatientsList=new ObservableCollection<Patient>(dto.Patients);
        DoctorsList=new ObservableCollection<Doctor>(dto.Doctors);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.AppointmentHistory);
        PatientDiagnosisHistory=new ObservableCollection<DiagnosisHistoryItem>(ctx.DiagnosisHistory);

        SelectedPatientForAppointment=
            PatientsList.FirstOrDefault(x => x.Id==Appointment.PatientId);

        SelectedDoctorForAppointment=
            DoctorsList.FirstOrDefault(x => x.Id==Appointment.DoctorId);

        SelectedTherapyCycle=
            VisibleTherapies.FirstOrDefault(x => x.Id==Appointment.TherapyCycleId);

        SelectedStartTime=Appointment.ScheduledStart.TimeOfDay;
        SelectedEndTime=Appointment.ScheduledEnd.TimeOfDay;
    }

    // =========================
    // PATIENT CHANGE
    // =========================

    public async Task OnPatientChangedAsync(Patient patient)
    {
        if(patient==null) return;

        SelectedPatientForAppointment=patient;

        var ctx = await _service.GetPatientContext(patient.Id);

        AppointmentHistory=new ObservableCollection<Appointment>(ctx.AppointmentHistory);
        PatientDiagnosisHistory=new ObservableCollection<DiagnosisHistoryItem>(ctx.DiagnosisHistory);
        VisibleTherapies=new ObservableCollection<TherapyCycle>(ctx.TherapyCycles);

        SelectedTherapyCycle=VisibleTherapies.FirstOrDefault();
    }

    [RelayCommand]
    private Task PatientChanged(Patient patient)
        => OnPatientChangedAsync(patient);

    // =========================
    // DIAGNOSIS SEARCH (FIXED CANCEL TOKEN)
    // =========================

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
        catch(TaskCanceledException)
        {
            // ignore
        }
    }

    [RelayCommand]
    private void AddDiagnosis(Mkb10Code code)
    {
        if(code==null) return;

        if(SelectedDiagnoses.Any(x => x.Mkb10CodeId==code.Id))
            return;

        SelectedDiagnoses.Add(new AppointmentDiagnosis
        {
            Mkb10CodeId=code.Id,
            DiagnosisId=code.Id,
            Mkb10Code=code,
            IsPrimary=false
        });

        DiagnosisSearchText=string.Empty;
        AvailableDiagnoses.Clear();
        ShowDiagnosisDropdown=false;
    }

    [RelayCommand]
    private void RemoveDiagnosis(AppointmentDiagnosis d)
    {
        if(d!=null)
            SelectedDiagnoses.Remove(d);
    }

    // =========================
    // EDIT MODE
    // =========================

    [RelayCommand]
    private void ToggleEditMode()
    {
        IsReadOnly=false;
        IsEditMode=true;
    }

    [RelayCommand]
    private async Task Cancel()
    {
        await _navigation.GoToAsync(AppRoutes.Appointments.List);
    }

    // =========================
    // SAVE (FIXED THERAPY + TIME BUG)
    // =========================

    [RelayCommand]
    private async Task SaveAsync()
    {
        Appointment.PatientId=SelectedPatientForAppointment?.Id??Guid.Empty;
        Appointment.DoctorId=SelectedDoctorForAppointment?.Id??Guid.Empty;

        Appointment.TherapyCycleId=SelectedTherapyCycle?.Id;

        Appointment.ScheduledStart=
            Appointment.ScheduledStart.Date+SelectedStartTime;

        Appointment.ScheduledEnd=
            Appointment.ScheduledEnd.Date+SelectedEndTime;

        await _service.SaveAppointment(
            Appointment,
            SelectedDiagnoses.ToList(),
            SelectedTherapyCycle);

        await _navigation.GoToAsync("..");
    }

    // =========================
    // HISTORY SELECT
    // =========================

    [RelayCommand]
    private async Task SelectAppointmentAsync(Appointment appointment)
    {
        if(appointment==null) return;

        _selected.SelectedItem=appointment;
        await LoadAsync();
    }

    // =========================
    // NEXT CYCLE
    // =========================

    [RelayCommand]
    private async Task GenerateNextCycleAsync()
    {
        await _service.GenerateNextTherapyCycle(Appointment);
        await LoadAsync();
    }
}