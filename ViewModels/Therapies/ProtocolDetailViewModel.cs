using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels;

public partial class ProtocolDetailFormViewModel(
    IDbContextFactory<DesktopTherapyDbContext> dbFactory,
    ISelectedItemService<TherapyProtocol> selectedItemService,
    INavigationService navigationService, IAuthStateService authStateService,
    IUserDialogService userDialogService,
    IAuthorizationService authorizationService,
    FormSessionLifecycle formSessions) : ObservableObject
{
    [ObservableProperty] private string _pageTitle = "Нов Протокол";
    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private TherapyProtocol _currentProtocol = new();

    private bool _hasInitialized;
    private bool _isCreateEntry;
    private TherapyProtocol? _loadedProtocol;

    public bool CanCreate => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Create);
    public bool CanUpdate => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Edit);
    public bool CanDelete => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Delete);
    public bool CanApprove => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Approve);
    public bool CanPrint => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Print);
    public bool CanExport => authorizationService.CanPerform(Modules.Protocols, ModuleAction.Export);
    public bool CanSave => IsEditMode ? CanUpdate : CanCreate;
    public bool CanEditForm => IsEditMode ? CanUpdate : CanCreate;

    public void OnPageAppearing()
    {
        var selected = selectedItemService.SelectedItem;

        // Consume the singleton navigation context once, otherwise a later
        // "New Protocol" navigation can accidentally reopen the previous protocol.
        if(selected is not null)
        {
            _loadedProtocol = selected;
            selectedItemService.SelectedItem = null;
        }
        else if(_hasInitialized && !_isCreateEntry)
        {
            // Keep the current record when this same Edit page appears again.
            selected = _loadedProtocol;
        }

        if(_hasInitialized && _isCreateEntry && selected is null)
            formSessions.BeginCreateEntry(this, ResetForNewProtocolSession);

        InitializeForm(selected);
        _hasInitialized = true;
        _isCreateEntry = selected is null;

        if(_isCreateEntry)
            formSessions.MarkEntryCompleted(this);
    }

    private void InitializeForm(TherapyProtocol? selected)
    {

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

    private void ResetForNewProtocolSession()
    {
        CurrentProtocol=new TherapyProtocol
        {
            CreatedAt=DateTime.UtcNow,
            CreatedByDoctor=authStateService.CurrentUser.Username
        };
        PageTitle="➕ Нов Протокол";
        IsEditMode=false;
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanEditForm));
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

            await navigationService.GoToAsync($"//{AppRoutes.Dashboard}");
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