using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterCreateViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Appointment> _appointmentContext; // од "Започни преглед" (Appointment)
    private readonly ISelectedItemService<Patient> _patientContext;         // од "Нов преглед" (Patient grid row)
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
        // Вчитува Patients/Doctors, креира нов Encounter, IsEditMode=true/IsReadOnly=false
        await InitializeAsync(null);
        Encounter.Status=EncounterStatus.Scheduled;

        // 1) Дојде преку "Започни преглед" од Appointment? — наследи целосен контекст
        var incomingAppointment = _appointmentContext.SelectedItem;
        if(incomingAppointment!=null&&incomingAppointment.Id!=Guid.Empty)
        {
            await ApplyAppointmentContextAsync(incomingAppointment);
            IsPatientLockedFromContext=false;
            _appointmentContext.SelectedItem=null;
            IsEditMode=true;
            IsReadOnly=false;
            return;
        }

        // 2) Дојде преку клик на ред од Patient grid ("Нов преглед за пациентот") — предпополни пациент
        var incomingPatient = _patientContext.SelectedItem;
        if(incomingPatient!=null&&incomingPatient.Id!=Guid.Empty)
        {
            var matchedPatient = Patients.FirstOrDefault(p => p.Id==incomingPatient.Id)??incomingPatient;
            IsPatientLockedFromContext=false;
            // Ова тригерира OnSelectedPatientChanged (reset + fire-and-forget load-и).
            SelectedPatient=matchedPatient;

            // Примарен лекар од пациентот, ако постои — не бара корисникот рачно да го избира.
            if(matchedPatient.DoctorId!=Guid.Empty)
                SelectedDoctor=Doctors.FirstOrDefault(d => d.Id==matchedPatient.DoctorId);

            // Awaitни ги истите load-и за да се пополнат картите (дијагнози, терапии,
            // appointments, циклуси) ПРЕД формата да се прикаже — нема друг конкурентен
            // UI повик во моментов, значи безбедно е.
            await LoadTherapyCyclesForPatientAsync(matchedPatient.Id);
            await LoadAppointmentsForPatientAsync(matchedPatient.Id);
            await LoadPatientContextAsync(matchedPatient.Id);

            _patientContext.SelectedItem=null; // consume
        }

        IsEditMode=true;
        IsReadOnly=false;
    }
    [RelayCommand]
    private void ToggleSection(string section)
    {
        if(string.IsNullOrWhiteSpace(section))
            return;

        ExpandedSection=ExpandedSection==section
            ? null
            : section;
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

            Encounter.Diagnoses.Clear();
            foreach(var diagnosis in Diagnoses)
                Encounter.Diagnoses.Add(diagnosis);

            await EncounterService.SaveEncounter(Encounter, Diagnoses.ToList(), Prescriptions.ToList());
            await NavigationService.GoToAsync(AppRoutes.Encounters.List);
        }, "Грешка при перзистирање на податоците за прегледот");
    }
}