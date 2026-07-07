using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Services;

namespace EHMR.ViewModels.Encounters;
public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    public EncounterEditViewModel(
        IEncounterDetailService service,
        ISelectedItemService<Encounter> selectedItemService)
        : base(service)
    {
              PageTitle="Промени Преглед";
        _selectedItemService=selectedItemService;
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
        IsEditMode=false;
        IsReadOnly=true;

        _selectedItemService.SelectedItem=null; // consume it — sprechava stale ID на следна навигација
    }
    [RelayCommand]
    public async Task SaveAsync()
    {
        await ExecuteSafeAsync(async () =>
        {
            await EncounterService.SaveEncounter(
                Encounter,
                Diagnoses.ToList(),
                Prescriptions.ToList());
        }, "Неусшено зачувување");
    }
}