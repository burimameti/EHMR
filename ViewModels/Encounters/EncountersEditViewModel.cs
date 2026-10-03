using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IPatientClinicalReportService _clinicalReportService;
    private readonly IAuthorizationService _authorizationService;
    public EncounterEditViewModel(
        IEncounterDetailService service,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        ISelectedItemService<Encounter> selectedItemService,
        IPatientClinicalReportService clinicalReportService,
        IAuthorizationService authorizationService)
        : base(service, navigationService, userDialogService)
    {
        _selectedItemService=selectedItemService;
        _clinicalReportService=clinicalReportService;
        _authorizationService=authorizationService;
        PageTitle="Промена на преглед";
    }

    [RelayCommand]
    private async Task GenerateClinicalReportAsync()
    {
        if(Encounter.PatientId==Guid.Empty) return;

        try
        {
            var path=await _clinicalReportService.GeneratePdfAsync(
                Encounter.PatientId,
                Encounter.Id,
                Encounter.AppointmentId,
                    $"Детален извештај - {SelectedPatient?.FullName??"Пациент"}");

            await Launcher.Default.OpenAsync(new OpenFileRequest(
                System.IO.Path.GetFileName(path),
                new ReadOnlyFile(path)));
        }
        catch(Exception ex)
        {
            await UserDialogService.ShowAlertAsync(
                "Извештај",
                $"PDF извештајот не може да се генерира: {ex.Message}",
                "ОК");
        }
    }

    private bool CanModifyEncounter =>
        _authorizationService.CanPerform("encounters", ModuleAction.Edit);

    public async Task LoadAsync()
    {
        var selected = _selectedItemService.SelectedItem;
        if(selected==null||selected.Id==Guid.Empty)
        {
            OnError("Не е избран преглед за промена.");
            return;
        }

        // InitializeAsync() already loads the patient context (Diagnoses, PatientMedicines
        // full history, etc.) via LoadPatientContextAsync for the existing-encounter path —
        // EncounterMedicines starts empty here, so only medicines the user adds/removes
        // during THIS edit session get touched on save; the rest of the patient's
        // medicine history is left completely alone.
        await InitializeAsync(selected.Id);

        EncounterMedicines=new System.Collections.ObjectModel.ObservableCollection<PatientMedicine>(
            PatientMedicines.Where(x => x.EncounterId==Encounter.Id));

        // load the cycle picker for this encounter's patient and preselect its current cycle
   

        IsEditMode=true;
        IsReadOnly=false;
        _selectedItemService.SelectedItem=null; // consume — prevents stale ID on next navigation
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if(!CanModifyEncounter)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за промена на овој преглед.", "ОК");
            return;
        }

        if(Encounter.Status==EncounterStatus.Completed||Encounter.IsLocked)
        {
            await UserDialogService.ShowAlertAsync("Заклучен преглед", "Завршен преглед не може да се менува.", "ОК");
            return;
        }

        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "Податоците за неактивен пациент се заклучени и не може да се менуваат.", "ОК");
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
            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList(),
                EncounterMedicines.ToList(),
                DeletedMedicineIds.ToList(),
                ScoreText);

            await UserDialogService.ShowMessageAsync("Податоци за преглед се успешно зачувани", "");
            await NavigationService.GoToAsync(AppRoutes.Encounters.List);
        }, "Неуспешно зачувување");
    }

    [RelayCommand]
    public async Task ChangeStatusAsync(EncounterStatus newStatus)
    {
        if(!CanModifyEncounter)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате овластување за промена на статусот на овој преглед.", "ОК");
            return;
        }

        if(Encounter.Status==EncounterStatus.Completed||Encounter.IsLocked)
        {
            await UserDialogService.ShowAlertAsync("Заклучен преглед", "Завршен преглед не може да се менува.", "ОК");
            return;
        }

        if(SelectedPatient?.Status==PatientStatus.Inactive)
        {
            await UserDialogService.ShowAlertAsync("Пациентот е неактивен", "Податоците за неактивен пациент се заклучени и не може да се менуваат.", "ОК");
            return;
        }

        if(Encounter.IsLocked&&newStatus!=EncounterStatus.Completed)
        {
            await UserDialogService.ShowAlertAsync(
                "Заклучен преглед",
                "Овој преглед е заклучен и не може да се менува.",
                "Во ред");
            return;
        }

        var previous = Encounter.Status;
        Encounter.Status=newStatus;

        // Lock the encounter when completed — clinical record is frozen
        if(newStatus==EncounterStatus.Completed)
            Encounter.IsLocked=true;

        await ExecuteSafeAsync(async () =>
        {
            await EncounterService.UpdateAppointmentStatus(
                Encounter.AppointmentId,
                MapToAppointmentStatus(newStatus));

        }, "Грешка при промена на статус");

        OnPropertyChanged(nameof(EncounterStatusDisplay));
    }


    private static AppointmentStatus MapToAppointmentStatus(EncounterStatus s) => s switch
    {
        EncounterStatus.Scheduled => AppointmentStatus.Scheduled,
        EncounterStatus.InProgress => AppointmentStatus.InProgress,
        EncounterStatus.Completed => AppointmentStatus.Completed,
        EncounterStatus.Cancelled => AppointmentStatus.Cancelled,
        _ => AppointmentStatus.Scheduled
    };
}
