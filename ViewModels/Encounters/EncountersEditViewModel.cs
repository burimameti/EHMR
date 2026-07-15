using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;

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

        await InitializeAsync(selected.Id);

        // load the cycle picker for this encounter's patient and preselect its current cycle
        await LoadTherapyCyclesForPatientAsync(Encounter.PatientId);

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

            await EncounterService.SaveEncounter(Encounter, Diagnoses.ToList(), Prescriptions.ToList());
            await UserDialogService.ShowMessageAsync("Податоци за преглед се успешно зачувани", "");
            await NavigationService.GoToAsync(AppRoutes.Encounters.List);
        }, "Неуспешно зачувување");
    }
}