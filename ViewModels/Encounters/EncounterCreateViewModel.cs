using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using EHMR.ViewModels.Encounters;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Encounters;
public partial class EncounterCreateViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Appointment> _appointmentContext;
    private readonly ISelectedItemService<Patient> _patientContext;

    [ObservableProperty]
    private string? expandedSection;

    public EncounterCreateViewModel(
        IEncounterDetailService service,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        ISelectedItemService<Appointment> appointmentContext,
        ISelectedItemService<Patient> patientContext)
        : base(service, navigationService, userDialogService)
    {
        _appointmentContext=appointmentContext;
        _patientContext=patientContext;
        PageTitle="Нов Преглед";
    }

    public async Task LoadAsync()
    {
        await InitializeAsync(null);
        Encounter.Status=EncounterStatus.Scheduled;

        // Flow 1: "Започни преглед" from Appointment row
        var incomingAppointment = _appointmentContext.SelectedItem;
        if(incomingAppointment!=null&&incomingAppointment.Id!=Guid.Empty)
        {
            await ApplyAppointmentContextAsync(incomingAppointment);
            _appointmentContext.SelectedItem=null;

            IsPatientLockedFromContext=true;  // ← was false: patient+doctor come from the appointment, lock them
            IsEditMode=true;
            IsReadOnly=false;
            return;
        }

        // Flow 2: "Нов преглед" from Patient grid row — walk-in, patient pre-filled
        var incomingPatient = _patientContext.SelectedItem;
        if(incomingPatient!=null&&incomingPatient.Id!=Guid.Empty)
        {
            var matchedPatient = Patients.FirstOrDefault(p => p.Id==incomingPatient.Id)
                                 ??incomingPatient;

            IsPatientLockedFromContext=true;  // ← was false: patient came from context, lock it
            SelectedPatient=matchedPatient;

            if(matchedPatient.DoctorId!=Guid.Empty)
                SelectedDoctor=Doctors.FirstOrDefault(d => d.Id==matchedPatient.DoctorId);

            await LoadTherapyCyclesForPatientAsync(matchedPatient.Id);
            await LoadAppointmentsForPatientAsync(matchedPatient.Id);
            await LoadPatientContextAsync(matchedPatient.Id);

            _patientContext.SelectedItem=null;
        }

        // Flow 3: Pure walk-in — everything selected manually
        IsPatientLockedFromContext=false;
        IsEditMode=true;
        IsReadOnly=false;
    }

    [RelayCommand]
    private void ToggleSection(string section)
    {
        if(string.IsNullOrWhiteSpace(section))
            return;

        ExpandedSection=ExpandedSection==section ? null : section;
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if(SelectedPatient is null||SelectedDoctor is null)
        {
            await UserDialogService.ShowAlertAsync(
                "Валидација",
                "Мора да изберете пациент и лекар пред зачувување.",
                "Во ред");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            Encounter.ClinicalNotes=EncounterDiagnosisNotes;
            Encounter.PatientId=SelectedPatient.Id;
            Encounter.DoctorId=SelectedDoctor.Id;
            Encounter.TherapyCycleId=SelectedTherapyCycle?.Id;

            // DO NOT populate Encounter.Diagnoses nav collection —
            // SaveEncounter clears it anyway and writes via db.Diagnoses directly.
            // Populating it here causes EF to attempt double-insert on tracked entities.
            Encounter.Diagnoses.Clear();

            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList(),
                EncounterMedicines.ToList(),
                DeletedMedicineIds.ToList());

            await NavigationService.GoToAsync(AppRoutes.Encounters.List);

        }, "Грешка при перзистирање на податоците за прегледот");
    }
}