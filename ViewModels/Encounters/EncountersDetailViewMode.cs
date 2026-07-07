using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Services;
namespace EHMR.ViewModels.Encounters;
public partial class EncounterDetailViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    public EncounterDetailViewModel(
        IEncounterDetailService service,
        ISelectedItemService<Encounter> selectedItemService)
        : base(service)
    {
        PageTitle="Детали за Преглед";
        _selectedItemService=selectedItemService;
    }

    public async Task LoadAsync()
    {
        var selected = _selectedItemService.SelectedItem;
        if(selected==null||selected.Id==Guid.Empty)
        {
            OnError("Не е избран преглед за прикажување.");
            return;
        }

        await InitializeAsync(selected.Id);
        IsEditMode=false;
        IsReadOnly=true;

        _selectedItemService.SelectedItem=null; // consume it — sprechava stale ID на следна навигација
    }
}