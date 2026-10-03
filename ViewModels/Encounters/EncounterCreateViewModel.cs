using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Helpers;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterCreateViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Appointment> _appointmentContext;
    private readonly ISelectedItemService<Patient> _patientContext;
    private readonly ISelectedItemService<Encounter> _encounterContext;
    private readonly IAuthorizationService _authorizationService;
    private bool _isLoading;
    private bool _isLoaded;

    // ── Side-panel ────────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Encounter> recentEncounters = [];

    [ObservableProperty]
    private ImageSource? patientPhotoSource;

    [ObservableProperty]
    private PatientDocument? latestPatientDocument;

    // ── Patient search ────────────────────────────────────────────────────────
    [ObservableProperty]
    private string patientSearchText = string.Empty;

    [ObservableProperty]
    private bool useCyrillicPatientSearch = true;

    [ObservableProperty]
    private ObservableCollection<Patient> patientSuggestions = [];

    [ObservableProperty]
    private Patient? patientSuggestionSelection;

    [ObservableProperty]
    private bool showPatientSuggestions;

    [ObservableProperty]
    private bool scheduleNextFollowUp;

    [ObservableProperty]
    private DateTime nextFollowUpDate=DateTime.Today.AddDays(7);

    [ObservableProperty]
    private string? selectedFollowUpInterval;

    public DateTime FollowUpMinimumDate => DateTime.Today;

    public ObservableCollection<string> FollowUpIntervalOptions { get; } =
        new()
        {
            "1 недела",
            "2 недели",
            "3 недели",
            "1 месец",
            "3 месеци",
            "6 месеци",
            "1 година"
        };

    partial void OnSelectedFollowUpIntervalChanged(string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return;
        NextFollowUpDate=value switch
        {
            "1 недела" => DateTime.Today.AddDays(7),
            "2 недели" => DateTime.Today.AddDays(14),
            "3 недели" => DateTime.Today.AddDays(21),
            "1 месец" => DateTime.Today.AddMonths(1),
            "3 месеци" => DateTime.Today.AddMonths(3),
            "6 месеци" => DateTime.Today.AddMonths(6),
            "1 година" => DateTime.Today.AddYears(1),
            _ => NextFollowUpDate
        };
    }

    // ── Medicine table visibility ─────────────────────────────────────────────
    public bool HasEncounterMedicines => EncounterMedicines.Count>0;

    // ── Constructor ───────────────────────────────────────────────────────────
    public EncounterCreateViewModel(
        IEncounterDetailService service,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        ISelectedItemService<Appointment> appointmentContext,
        ISelectedItemService<Patient> patientContext,
        ISelectedItemService<Encounter> encounterContext,
        IAuthorizationService authorizationService)
        : base(service, navigationService, userDialogService, authorizationService)
    {
        _appointmentContext=appointmentContext;
        _patientContext=patientContext;
        _encounterContext=encounterContext;
        _authorizationService=authorizationService;
        PageTitle="Нов Преглед";

        EncounterMedicines.CollectionChanged+=OnEncounterMedicinesChanged;
    }

    private void OnEncounterMedicinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => OnPropertyChanged(nameof(HasEncounterMedicines));

    // ── Load ──────────────────────────────────────────────────────────────────
    public async Task LoadAsync()
    {
        if(_isLoading||_isLoaded) return;
        _isLoading=true;
        try
        {
            await InitializeAsync(null);
            var incomingAppointment=_appointmentContext.SelectedItem;
            if(incomingAppointment!=null&&incomingAppointment.Id!=Guid.Empty)
            {
                await ApplyAppointmentContextAsync(incomingAppointment);
                var duration=Math.Max(1, (int)(incomingAppointment.ScheduledEnd-incomingAppointment.ScheduledStart).TotalMinutes);
                Encounter.Schedule(incomingAppointment.ScheduledStart, duration);
                OnPropertyChanged(nameof(AutomaticScheduleDisplay));
                _appointmentContext.SelectedItem=null;
                IsPatientLockedFromContext=true;
                IsEditMode=true;
                IsReadOnly=false;
                LoadCurrentMedicinesForEncounter();
                RefreshSidePanel();
                _isLoaded=true;
                return;
            }
            var incomingPatient=_patientContext.SelectedItem;
            if(incomingPatient!=null&&incomingPatient.Id!=Guid.Empty)
            {
                var matchedPatient=Patients.FirstOrDefault(p => p.Id==incomingPatient.Id)??incomingPatient;
                IsPatientLockedFromContext=true;
                _isApplyingContext=true;
                try
                {
                    SelectedPatient=matchedPatient;
                    if(matchedPatient.DoctorId!=Guid.Empty)
                        SelectedDoctor=Doctors.FirstOrDefault(d => d.Id==matchedPatient.DoctorId);
                    await LoadTherapyCyclesForPatientAsync(matchedPatient.Id);
                    await LoadAppointmentsForPatientAsync(matchedPatient.Id);
                    await LoadPatientContextAsync(matchedPatient.Id);
                    LoadCurrentMedicinesForEncounter();
                }
                finally { _isApplyingContext=false; }
                _patientContext.SelectedItem=null;
                RefreshSidePanel();
                _isLoaded=true;
                return;
            }
            IsPatientLockedFromContext=false;
            IsEditMode=true;
            IsReadOnly=false;
            _isLoaded=true;
        }
        finally { _isLoading=false; }
    }
    // ── Patient search ────────────────────────────────────────────────────────
    partial void OnPatientSuggestionSelectionChanged(Patient? value)
    {
        if(value is not null)
            SelectPatientSuggestionCommand.Execute(value);
    }

    partial void OnPatientSearchTextChanged(string value)
    {
        var query = value?.Trim()??string.Empty;

        if(query.Length<1||IsPatientLockedFromContext)
        {
            PatientSuggestions.Clear();
            ShowPatientSuggestions=false;
            return;
        }

        var cyrillicQuery = UseCyrillicPatientSearch
            ? MacedonianTransliterator.ToCyrillic(query)
            : query;
        var suggestions = Patients
            .Where(p =>
                p.FullName?.Contains(query, StringComparison.OrdinalIgnoreCase)==true||
                p.FullName?.Contains(cyrillicQuery, StringComparison.OrdinalIgnoreCase)==true||
                p.PatientNumber?.Contains(query, StringComparison.OrdinalIgnoreCase)==true||
                p.NationalId?.Contains(query, StringComparison.OrdinalIgnoreCase)==true||
                p.SzboNumber?.Contains(query, StringComparison.OrdinalIgnoreCase)==true)
            .OrderBy(p => p.FullName)
            .Take(10);

        PatientSuggestions=new ObservableCollection<Patient>(suggestions);
        ShowPatientSuggestions=PatientSuggestions.Count>0;
    }

    partial void OnUseCyrillicPatientSearchChanged(bool value)
        => OnPatientSearchTextChanged(PatientSearchText);

    [RelayCommand]
    private async Task SelectPatientSuggestion(Patient? patient)
    {
        if(patient is null) return;
        PatientSearchText=patient.FullName;
        ShowPatientSuggestions=false;
        _isApplyingContext=true;
        try
        {
            SelectedPatient=patient;
            if(patient.DoctorId!=Guid.Empty)
                SelectedDoctor=Doctors.FirstOrDefault(d => d.Id==patient.DoctorId);
            await LoadTherapyCyclesForPatientAsync(patient.Id);
            await LoadAppointmentsForPatientAsync(patient.Id);
            await LoadPatientContextAsync(patient.Id);
            LoadCurrentMedicinesForEncounter();
        }
        finally { _isApplyingContext=false; }
        RefreshSidePanel();
    }

    private void LoadCurrentMedicinesForEncounter()
    {
        if(EncounterMedicines.Count>0) return;

        // Only patient-level active therapy is carried into a new encounter.
        // Encounter-scoped rows are historical snapshots and must never appear
        // as preselected current therapy.
        foreach(var medicine in PatientMedicines
            .Where(x => x.IsActive && x.EncounterId==null)
            .OrderBy(x => x.StartDate))
        {
            EncounterMedicines.Add(new PatientMedicine
            {
                Id=Guid.NewGuid(),
                PatientId=Encounter.PatientId,
                EncounterId=Encounter.Id==Guid.Empty ? null : Encounter.Id,
                MedicineId=medicine.MedicineId,
                Medicine=medicine.Medicine,
                ApplicationRegimeId=medicine.ApplicationRegimeId,
                ApplicationRegime=medicine.ApplicationRegime,
                Quantity=0,
                Dosage=medicine.Dosage,
                DosesFrequency=medicine.DosesFrequency,
                StartDate=DateTime.Now,
                EndDate=null,
                Notes=medicine.Notes,
                PharmaceuticalReference=medicine.PharmaceuticalReference,
                IsActive=true
            });
        }
    }

    // ── Schedule display ──────────────────────────────────────────────────────
    public string AutomaticScheduleDisplay =>
        Encounter.ScheduledStart is { } start
            ? $"{start:dd.MM.yyyy HH:mm} – {Encounter.ScheduledEnd:HH:mm}"
            : "Се пресметува...";

    private async Task AssignNextAvailableSlotAsync()
    {
        if(SelectedDoctor is null) return;

        var start = await EncounterService.GetNextAvailableSlot(
            SelectedDoctor.Id,
            DateTime.Now,
            30);

        Encounter.Schedule(start);

        OnPropertyChanged(nameof(StatusDisplay));
        OnPropertyChanged(nameof(ScheduledStartDate));
        OnPropertyChanged(nameof(ScheduledStartTime));
        OnPropertyChanged(nameof(AutomaticScheduleDisplay));
    }

    // ── Side panel ────────────────────────────────────────────────────────────
    protected void OnSelectedPatientChanged(Patient? value)
    {
        if(value is not null)
            RefreshSidePanel();
        else
        {
            RecentEncounters.Clear();
            PatientPhotoSource=null;
        }
    }

    private void RefreshSidePanel()
    {
        RecentEncounters=new ObservableCollection<Encounter>(
            PatientEncounters
                .OrderByDescending(e => e.EncounterDate));

        var photo = PatientDocuments
            .Where(d => !d.IsDeleted&&
                        d.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefault();

        LatestPatientDocument=PatientDocuments
            .Where(d => !d.IsDeleted)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefault();

        PatientPhotoSource=photo is not null
            ? ImageSource.FromFile(photo.StoredPath)
            : null;
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private void ClearAppointment()
    {
        LinkedAppointment=null;
        SelectedAppointment=null;
        Encounter.AppointmentId=null;
    }

    [RelayCommand]
    private void ClearTherapyCycle()
    {
        SelectedTherapyCycle=null;
        Encounter.TherapyCycleId=null;
    }

    [RelayCommand]
    private async Task PreviewDocument(PatientDocument doc)
    {
        if(doc is null||string.IsNullOrWhiteSpace(doc.StoredPath)) return;
        if(!File.Exists(doc.StoredPath))
        {
            await UserDialogService.ShowAlertAsync("Документ", "Документот не е пронајден на дискот.", "Во ред");
            return;
        }

        await Launcher.Default.OpenAsync(new OpenFileRequest(
            doc.Title,
            new ReadOnlyFile(doc.StoredPath)));
    }

    [RelayCommand]
    private async Task OpenEncounter(Encounter encounter)
    {
        if(encounter is null||encounter.Id==Guid.Empty) return;
        _encounterContext.SelectedItem=encounter;
        _encounterContext.OpenInEditMode=false;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    [RelayCommand]
    private async Task SaveEncounter()
    {
        if(!_authorizationService.CanPerform("encounters", ModuleAction.Create))
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за креирање прегледи.", "ОК");
            return;
        }

        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "За неактивен пациент не може да се креира или менува преглед.", "ОК");
            return;
        }

        if(SelectedPatient is null||SelectedDoctor is null)
        {
            await UserDialogService.ShowAlertAsync(
                "Валидација",
                "Мора да изберете пациент и реуматолог пред зачувување.",
                "Во ред");
            return;
        }

                if(EncounterMedicines.Any(x => string.IsNullOrWhiteSpace(x.ApplicationRegime?.Regime)))
        {
            await UserDialogService.ShowAlertAsync(
                "Валидација",
                "За секој додаден лек мора да изберете режим на апликација.",
                "Во ред");
            return;
        }

        ResolveApplicationRegimes(EncounterMedicines);

        await ExecuteSafeAsync(async () =>
        {
            Encounter.SetNotes(EncounterDiagnosisNotes);
            Encounter.PatientId=SelectedPatient.Id;
            Encounter.DoctorId=SelectedDoctor.Id;
            Encounter.TherapyCycleId=SelectedTherapyCycle?.Id;
            Encounter.Diagnoses.Clear();

            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList(),
                EncounterMedicines.ToList(),
                DeletedMedicineIds.ToList(),
                ScoreText,
                ScheduleNextFollowUp ? NextFollowUpDate.Date.AddHours(9) : null);

            await NavigationService.GoToAsync(AppRoutes.Encounters.List);

        }, "Грешка при перзистирање на податоците за прегледот");
    }

    [RelayCommand]
    private async Task Cancel()
    {
        SelectedItemService.SelectedItem=null;
        await NavigationService.GoBackAsync();
    }
}