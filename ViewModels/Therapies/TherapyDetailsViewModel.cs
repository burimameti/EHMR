using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Therapies;

public partial class TherapyDetailsViewModel : ObservableObject
{
    private readonly ITherapyService _therapyService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthorizationService _authService;

    public ObservableCollection<TherapyCycle> Cycles { get; } = new();

    [ObservableProperty] private TherapyCycle selectedCycle;
    [ObservableProperty] private Patient patient;
    [ObservableProperty] private int cycleNumber = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private bool isReadOnly;

    public bool IsEditMode => !IsReadOnly;

    public bool CanView => _authService.CanPerform(Modules.Therapy, ModuleAction.View);
    public bool CanCreate => _authService.CanPerform(Modules.Therapy, ModuleAction.Create);
    public bool CanUpdate => _authService.CanPerform(Modules.Therapy, ModuleAction.Edit);
    public bool CanDelete => _authService.CanPerform(Modules.Therapy, ModuleAction.Delete);

    public TherapyDetailsViewModel(
        ITherapyService therapyService,
        INavigationService navigationService,
        IUserDialogService dialogService,
        IAuthorizationService authService)
    {
        _therapyService=therapyService;
        _navigationService=navigationService;
        _dialogService=dialogService;
        _authService=authService;

        IsReadOnly=!CanUpdate;
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigationService.GoBackAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            Cycles.Clear();

            var cycles = await _therapyService.GetCyclesAsync();

            foreach(var c in cycles)
                Cycles.Add(c);
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка", ex.Message, "OK");
        }
    }

    [RelayCommand]
    private async Task CreateCycleAsync()
    {
        if(Patient==null || !CanCreate) return;

        var cycle = new TherapyCycle
        {
            Id=Guid.NewGuid(),

            PatientId=Patient.Id,
            Status=TherapyStatus.Planned
        };

        await _therapyService.AddCycleAsync(cycle);

        Cycles.Add(cycle);

        await _dialogService.ShowAlertAsync("Успешно", "Податоците се зачувани.", "OK");
    }

    [RelayCommand]
    private async Task SaveCycleAsync()
    {
        if(SelectedCycle==null || !CanUpdate) return;

        await _therapyService.UpdateCycleAsync(SelectedCycle);

        await _dialogService.ShowAlertAsync("Успешно", "Податоците се зачувани.", "OK");
    }

    [RelayCommand]
    private async Task DeleteCycleAsync()
    {
        if(SelectedCycle==null || !CanDelete) return;

        await _therapyService.DeleteCycleAsync(SelectedCycle.Id);

        Cycles.Remove(SelectedCycle);
        SelectedCycle=null;
    }

    public List<string> StatusOptions =>
        Enum.GetNames(typeof(TherapyStatus)).ToList();
}