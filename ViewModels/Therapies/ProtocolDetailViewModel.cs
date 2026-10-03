using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels;

public partial class ProtocolDetailFormViewModel(
    IDbContextFactory<DesktopTherapyDbContext> dbFactory,
    ISelectedItemService<TherapyProtocol> selectedItemService,
    INavigationService navigationService, IAuthStateService authStateService,
    IUserDialogService userDialogService,
    IAuthorizationService authorizationService) : ObservableObject
{
    [ObservableProperty] private string _pageTitle = "Нов Протокол";
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private TherapyProtocol _currentProtocol;

    public bool CanCreate => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Create);
    public bool CanUpdate => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Edit);
    public bool CanDelete => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Delete);
    public bool CanApprove => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Approve);
    public bool CanPrint => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Print);
    public bool CanExport => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Export);
    public bool CanSave => IsEditMode ? CanUpdate : CanCreate;
    public bool CanEditForm => IsEditMode ? CanUpdate : CanCreate;

    public void InitializeForm()
    {
        var selected = selectedItemService.SelectedItem;

        if(selected==null)
        {
            PageTitle="➕ Нов Протокол";
            IsEditMode=false;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanEditForm));
            CurrentProtocol=new TherapyProtocol
            {
                CreatedAt=DateTime.UtcNow,
                CreatedByDoctor=authStateService.CurrentUser.Username
            };
        }
        else
        {
            PageTitle=$"✏️ Измена на Протокол: {selected.Name}";
            IsEditMode=true;
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanEditForm));
            CurrentProtocol=selected;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(!CanSave)
            return;

        if(string.IsNullOrWhiteSpace(CurrentProtocol.Name)||string.IsNullOrWhiteSpace(CurrentProtocol.DiseaseCategory))
        {
            await userDialogService.ShowAlertAsync("Валидација", "Името и Категоријата на болеста се задолжителни полиња.", "OK");
            return;
        }

        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();

            if(!IsEditMode)
            {
                db.TherapyProtocols.Add(CurrentProtocol);
            }
            else
            {
                db.TherapyProtocols.Update(CurrentProtocol);
            }

            await db.SaveChangesAsync();
            await userDialogService.ShowAlertAsync("Успешно", "Протоколот е зачуван во каталогот.", "OK");

            await navigationService.GoToAsync(AppRoutes.Protocols.List);
        }
        catch(Exception ex)
        {
            await userDialogService.ShowAlertAsync("Грешка", $"Неуспешно зачувување: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await navigationService.GoToAsync(AppRoutes.Protocols.List);
    }
}