using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using EHMR.Domain.Entities.Rbac;
using EHMR.Services.Dto;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class ApplicationRegimeListViewModel : ObservableObject
{
    private readonly IPatientService _patientService;
    private readonly IUserDialogService _userDialogService;
    private readonly IAuthorizationService _authorization;

    [ObservableProperty] private ObservableCollection<ApplicationRegimeDto> regimes = new();
    [ObservableProperty] private string newRegime = string.Empty;
    [ObservableProperty] private ApplicationRegimeDto? editingRegime;
    [ObservableProperty] private bool isBusy;

    public ApplicationRegimeListViewModel(
        IPatientService patientService,
        IUserDialogService userDialogService,
        IAuthorizationService authorization)
    {
        _patientService=patientService;
        _userDialogService=userDialogService;
        _authorization=authorization;
    }

    public bool CanManage => _authorization.CanPerform(Modules.Administration, ModuleAction.Manage);

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy || !CanManage) return;
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
    public async Task EditAsync(ApplicationRegimeDto? item)
    {
        if(item is null || !CanManage) return;

        var value=await _userDialogService.ShowPromptAsync(
            "Измени режим",
            "Внесете нов назив на начинот на апликација.",
            "Зачувај",
            "Откажи",
            "Начин на апликација",
            item.Regime);

        if(string.IsNullOrWhiteSpace(value)||string.Equals(value.Trim(),item.Regime,StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            var updated=await _patientService.UpdateApplicationRegimeAsync(item.Id,value.Trim());
            var index=Regimes.IndexOf(item);
            if(index>=0)
                Regimes[index]=updated;
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Неуспешна промена: {ex.Message}", "ОК");
        }
    }

    [RelayCommand]
    public async Task DeleteAsync(ApplicationRegimeDto? item)
    {
        if(item is null || !CanManage) return;

        var confirmed=await _userDialogService.ShowConfirmationAsync(
            "Избриши начин на апликација",
            $"Дали сте сигурни дека сакате да го избришете „{item.Regime}“?",
            "Избриши",
            "Откажи");

        if(!confirmed) return;

        try
        {
            if(await _patientService.DeleteApplicationRegimeAsync(item.Id))
                Regimes.Remove(item);
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Неуспешно бришење: {ex.Message}", "ОК");
        }
    }

    [RelayCommand]
    public async Task AddAsync()
    {
        if(!CanManage) return;
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
