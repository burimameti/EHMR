using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels.Prescriptions;

public partial class PrescriptionListViewModel : BaseViewModel<Prescription>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Prescription> _selectedItem;

    /// <summary>Module key used by BaseViewModel&lt;T&gt;.EvaluatePermissions().</summary>
    protected override string ModuleName => "prescriptions";

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

        EvaluatePermissions(); // base method — was never being called before
    }

    // ================= LOAD =================
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

            AllItems=prescriptions??new();

            ApplyPipeline();
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

    // ================= NAVIGATION =================
    [RelayCommand]
    private async Task AddAsync()
    {
        _selectedItem.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(Prescription item)
    {
        if(item==null) return;

        _selectedItem.SelectedItem=item;
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Prescription item)
    {
        if(item==null) return;

        _selectedItem.SelectedItem=item;
        await NavigationService.GoToAsync(AppRoutes.Prescriptions.Detail);
    }

    // ================= DELETE =================
    [RelayCommand]
    private async Task DeleteAsync(Prescription item)
    {
        if(item==null) return;

        var confirm = await UserDialogService.ShowConfirmationAsync(
            "Потврда",
            "Дали сакате да ја избришете оваа рецепта?");

        if(!confirm) return;

        try
        {
            IsBusy=true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            db.Prescriptions.Remove(item);
            await db.SaveChangesAsync();

            AllItems.Remove(item);
            ApplyPipeline();

            await UserDialogService.ShowAlertAsync(
                "Успешно",
                "Рецептот е отстранет",
                "OK");
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

    // ================= FILTER HOOK =================
    protected override IEnumerable<Prescription> ApplyFilters(IEnumerable<Prescription> query)
    {
        // No filters yet → clean extension point
        return query;
    }

    // ClearFilters command now comes from BaseViewModel<T> (ClearFiltersCommand):
    // it calls ResetFilters() → ApplyPipeline() → SyncSparkPickersFromFilters().
    protected override void ResetFilters()
    {
        SearchText=string.Empty;
    }
}