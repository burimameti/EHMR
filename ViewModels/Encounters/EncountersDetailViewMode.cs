using EHMR.Services;
using EHMR.ViewModels.Encounters;
namespace EHMR.ViewModels.Encounters;
public partial class EncounterDetailViewModel : EncounterBaseViewModel
{
    public EncounterDetailViewModel(IEncounterDetailService service)
        : base(service)
    {
    }

    public async Task LoadAsync(Guid id)
    {
        await InitializeAsync(id);

        IsEditMode=false;
        IsReadOnly=true;
    }
}