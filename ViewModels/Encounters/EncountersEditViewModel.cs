using CommunityToolkit.Mvvm.Input;
using EHMR.Services;
using EHMR.ViewModels.Encounters;
namespace EHMR.ViewModels.Encounters;
public partial class EncounterEditViewModel : EncounterBaseViewModel
{
    public EncounterEditViewModel(IEncounterDetailService service)
        : base(service)
    {
    }

    public async Task LoadAsync(Guid id)
    {
        await InitializeAsync(id);

        IsEditMode=true;
        IsReadOnly=false;
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
        }, "Unable to save encounter");
    }
}