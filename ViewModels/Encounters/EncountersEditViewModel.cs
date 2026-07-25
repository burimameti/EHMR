using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly List<Guid> _deletedMedicineIds = [];
    protected IReadOnlyList<Guid> DeletedMedicineIds => _deletedMedicineIds;
    public EncounterEditViewModel(
        IEncounterDetailService service,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        ISelectedItemService<Encounter> selectedItemService)
        : base(service, navigationService, userDialogService)
    {
        _selectedItemService=selectedItemService;
        PageTitle="Промена на преглед";
    }

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

        // load the cycle picker for this encounter's patient and preselect its current cycle
   

        IsEditMode=true;
        IsReadOnly=false;
        _selectedItemService.SelectedItem=null; // consume — prevents stale ID on next navigation
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            Encounter.TherapyCycleId=SelectedTherapyCycle?.Id;

            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList(),
                EncounterMedicines.ToList(),
                DeletedMedicineIds.ToList());

            _deletedMedicineIds.Clear();

            await UserDialogService.ShowMessageAsync("Податоци за преглед се успешно зачувани", "");
            await NavigationService.GoToAsync(AppRoutes.Encounters.List);
        }, "Неуспешно зачувување");
    }

    [RelayCommand]
    public async Task ChangeStatusAsync(EncounterStatus newStatus)
    {
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
        EncounterStatus.CheckedIn => AppointmentStatus.CheckedIn,
        EncounterStatus.InProgress => AppointmentStatus.InProgress,
        EncounterStatus.Completed => AppointmentStatus.Completed,
        EncounterStatus.Cancelled => AppointmentStatus.Cancelled,
        EncounterStatus.NoShow => AppointmentStatus.Missed,
        _ => AppointmentStatus.Scheduled
    };
}