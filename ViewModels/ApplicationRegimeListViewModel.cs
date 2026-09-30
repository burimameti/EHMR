using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Services.Dto;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class ApplicationRegimeListViewModel : ObservableObject
{
    private readonly IPatientService _patientService;
    private readonly IUserDialogService _userDialogService;

    [ObservableProperty] private ObservableCollection<ApplicationRegimeDto> regimes = new();
    [ObservableProperty] private string newRegime = string.Empty;
    [ObservableProperty] private bool isBusy;

    public ApplicationRegimeListViewModel(
        IPatientService patientService,
        IUserDialogService userDialogService)
    {
        _patientService=patientService;
        _userDialogService=userDialogService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            Regimes=new ObservableCollection<ApplicationRegimeDto>(
                await _patientService.GetApplicationRegimesAsync());
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Неуспешно вчитување: {ex.Message}", "ОК");
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    public async Task AddAsync()
    {
        var value=(NewRegime??string.Empty).Trim();
        if(string.IsNullOrWhiteSpace(value))
        {
            await _userDialogService.ShowAlertAsync("Режим на апликација", "Внесете текст за режимот.", "ОК");
            return;
        }

        try
        {
            var item=await _patientService.AddApplicationRegimeAsync(value);
            if(!Regimes.Any(x => x.Id==item.Id))
                Regimes.Add(item);
            NewRegime=string.Empty;
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Неуспешно зачувување: {ex.Message}", "ОК");
        }
    }
}
