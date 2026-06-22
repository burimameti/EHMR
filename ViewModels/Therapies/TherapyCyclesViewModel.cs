using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Therapies;

public partial class TherapyCyclesViewModel : ObservableObject
{
    private readonly ITherapyService _therapyService;
    private readonly IAuthorizationService _authService;
    private readonly IUserDialogService _dialogService;

    private List<TherapyCycle> _allCycles = new();

    public ObservableCollection<TherapyCycle> FilteredCycles { get; } = new();

    [ObservableProperty] private TherapyCycle selectedCycle;
    [ObservableProperty] private Appointment selectedAppointment;

    [ObservableProperty] private bool isReadOnly;

    [ObservableProperty] private string searchText;
    [ObservableProperty] private string selectedStatusFilter;

    public TherapyCyclesViewModel(
        ITherapyService therapyService,
        IAuthorizationService authService,
        IUserDialogService dialogService)
    {
        _therapyService=therapyService;
        _authService=authService;
        _dialogService=dialogService;

        EvaluatePermissions();
    }

    private void EvaluatePermissions()
    {
        IsReadOnly=!_authService.CanAccessModule(Modules.Therapy);
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            _allCycles=await _therapyService.GetCyclesAsync();
            ApplyFilters();
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Error", ex.Message, "OK");
        }
    }

    private void ApplyFilters()
    {
        var result = _allCycles.AsEnumerable();

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            result=result.Where(c =>
                c.Patient.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if(!string.IsNullOrWhiteSpace(SelectedStatusFilter))
        {
            result=result.Where(c => c.Status.ToString()==SelectedStatusFilter);
        }

        FilteredCycles.Clear();

        foreach(var item in result)
            FilteredCycles.Add(item);
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText=string.Empty;
        SelectedStatusFilter=null;
        ApplyFilters();
    }
}