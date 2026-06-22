using CommunityToolkit.Mvvm.Input;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionListViewModel : BaseViewModel<Prescription>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Prescription> _selectedItem;

    public PrescriptionListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<Prescription> selectedItem)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItem=selectedItem;
    }

    public override async Task OnAppearingAsync()
    {
        await LoadAsync();
    }

    protected override async Task OnRefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var prescriptions = await db.Prescriptions
                .AsNoTracking()
                .Include(x => x.Patient)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            _allItems=prescriptions;

            await ApplyFilterAsync();
        }
        catch(Exception ex)
        {
            OnError($"Failed to load prescriptions: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        _selectedItem.SelectedItem=null; // Нов термин
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(Prescription appointment)
    {
        if(appointment==null) return;
        _selectedItem.SelectedItem=appointment;
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Prescription appointment)
    {
        if(appointment==null) return;
        _selectedItem.SelectedItem=appointment;
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    [RelayCommand]
    private async Task DeleteAsync(Prescription pr)
    {
        if(pr==null) return;

        bool confirm = await UserDialogService.ShowConfirmationAsync("Потврда", "Дали сакате да го откажете/избришетe?");
        if(!confirm) return;

        try
        {
            IsBusy=true;
            await using var db = await _dbFactory.CreateDbContextAsync();

            db.Prescriptions.Remove(pr);
            await db.SaveChangesAsync();

            _allItems.Remove(pr);
            await ApplyFilterAsync();

            await UserDialogService.ShowAlertAsync("Успешно", "Oтстранет од база", "OK");
        }
        catch(Exception ex)
        {
            OnError($"Грешка при бришење: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText=string.Empty;

        await ApplyFilterAsync();
    }
}