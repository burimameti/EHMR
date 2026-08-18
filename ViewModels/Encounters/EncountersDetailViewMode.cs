using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;
using System.IO;
namespace EHMR.ViewModels.Encounters;
public partial class EncounterDetailViewModel : EncounterBaseViewModel
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly ISelectedItemService<Patient> _selectedPatientService;

    public bool CanEditEncounter =>
        Encounter.Id!=Guid.Empty&&Encounter.Status!=EncounterStatus.Completed;

    public EncounterDetailViewModel(
        IEncounterDetailService service, INavigationService navigationService,
        IUserDialogService userDialogService, ISelectedItemService<Encounter> selectedItemService,
        ISelectedItemService<Patient> selectedPatientService)

        : base(service, navigationService, userDialogService)
    {
        _selectedItemService=selectedItemService;
        _selectedPatientService=selectedPatientService;
        PageTitle="Детали за преглед ";
    }

    public async Task LoadAsync()
    {
        var selectedEncounter = _selectedItemService.SelectedItem;

        if(selectedEncounter!=null)
        {
            // EXISTING ENCOUNTER — unchanged path
            await LoadForViewAsync(selectedEncounter.Id, "Не е избран преглед за прикажување.");
            EncounterMedicines=new ObservableCollection<PatientMedicine>(
                PatientMedicines.Where(x => x.EncounterId==Encounter.Id).OrderByDescending(x => x.StartDate));
            _selectedItemService.SelectedItem=null;
            OnPropertyChanged(nameof(CanEditEncounter));
            return;   // ← излегува тука, не стигнува до "нов" делот подолу
        }

        // NEW ENCOUNTER — само кога selectedEncounter е null
        var preselectedPatient = _selectedPatientService.SelectedItem;

        await InitializeAsync(null);

        if(preselectedPatient!=null)
        {
            SelectedPatient=Patients.FirstOrDefault(p => p.Id==preselectedPatient.Id)??preselectedPatient;
            SelectedDoctor = SelectedPatient.Doctor;
            _selectedPatientService.SelectedItem=null;
        }
    }    
    [RelayCommand]
    private async Task PreviewDocument(PatientDocument doc)
    {
        if(doc is null||string.IsNullOrWhiteSpace(doc.StoredPath)||!File.Exists(doc.StoredPath))
            return;

        await Launcher.Default.OpenAsync(new OpenFileRequest(
            doc.Title,
            new ReadOnlyFile(doc.StoredPath)));
    }
    [RelayCommand]
    private async Task Edit()
    {
        if(!CanEditEncounter)
            return;

        _selectedItemService.SelectedItem=Encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Edit);
    }

    [RelayCommand]
    private async Task OpenEncounter(Encounter encounter)
    {
        if(encounter is null)
            return;

        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }}
