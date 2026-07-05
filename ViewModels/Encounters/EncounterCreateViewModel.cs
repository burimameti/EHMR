using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Services;

namespace EHMR.ViewModels.Encounters;

public partial class EncounterCreateViewModel : EncounterBaseViewModel
{
    public EncounterCreateViewModel(IEncounterDetailService service)
        : base(service)
    {
        PageTitle="Нов Преглед";
    }

    public async Task LoadAsync()
    {
        await InitializeAsync(null);

        // Статусот автоматски се конфигурира како 'Scheduled' овде зад сцената
        Encounter=new Encounter
        {
            Id=Guid.NewGuid(),
            Status=EncounterStatus.Scheduled,
            CreatedAt=DateTime.Now
        };

        IsEditMode=true;
        IsReadOnly=false;
    }
    [RelayCommand]
    public async Task SaveAsync()
    {
        if(SelectedPatient is null||SelectedDoctor is null)
        {
            await Shell.Current.DisplayAlert("Валидација", "Мора да изберете пациент и лекар пред зачувување.", "Во ред");
            return;
        }

        await ExecuteSafeAsync(async () =>
        {
            try
            {
                Encounter.ClinicalNotes=EncounterDiagnosisNotes;
                Encounter.PatientId=SelectedPatient.Id;
                Encounter.DoctorId=SelectedDoctor.Id;

                Encounter.Diagnoses.Clear();
                foreach(var diagnosis in Diagnoses)
                {
                    Encounter.Diagnoses.Add(diagnosis);
                }

                await EncounterService.SaveEncounter(Encounter, Diagnoses.ToList(), Prescriptions.ToList());
                await Shell.Current.GoToAsync(AppRoutes.Encounters.List);
            }
            catch(Exception ex)
            {
                // Full details for debugging — remove/trim the stack trace once stable
                System.Diagnostics.Debug.WriteLine($"SaveAsync failed: {ex}");
                await Shell.Current.DisplayAlert(
                    "Грешка при зачувување",
                    $"{ex.GetType().Name}: {ex.Message}\n\n{ex.StackTrace}",
                    "Во ред");
                throw; // rethrow so ExecuteSafeAsync also records it in ErrorMessage/HasError
            }
        }, "Грешка при перзистирање на податоците за прегледот");
    }
}