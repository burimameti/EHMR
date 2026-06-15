using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.ViewModels;

public partial class MedicineDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Medicine> _medicineSelectionService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _userDialogService;

    [ObservableProperty] private Medicine _medicine = null!;
    [ObservableProperty] private string _pageTitle = string.Empty;
    [ObservableProperty] private bool _isReadOnly = true;

    private bool _isNewMode = false;
    public bool IsEditMode => !IsReadOnly;

    public MedicineDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Medicine> medicineSelectionService,
        INavigationService navigationService,
        IUserDialogService userDialogService)
    {
        _dbFactory=dbFactory;
        _medicineSelectionService=medicineSelectionService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;

        InitializeForm();
    }

    private void InitializeForm()
    {
        var selected = _medicineSelectionService.SelectedItem;
        if(selected==null)
        {
            Medicine=new Medicine();
            PageTitle="➕ Додај нов лек во шифрарник";
            IsReadOnly=false;
            _isNewMode=true;
        }
        else
        {
            Medicine=selected;
            PageTitle=$"Преглед: {Medicine.Name}";
            IsReadOnly=true;
            _isNewMode=false;
        }
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsReadOnly=false; PageTitle=$"Измени: {Medicine.Name}";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(string.IsNullOrWhiteSpace(Medicine.Name))
        {
            await _userDialogService.ShowAlertAsync("Грешка", "Името на лекот е задолжително!", "ОК");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            if(_isNewMode) await db.Set<Medicine>().AddAsync(Medicine);
            else db.Set<Medicine>().Update(Medicine);

            await db.SaveChangesAsync();
            await _navigationService.GoToAsync("..");
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка при зачувување", ex.Message, "ОК");
        }
    }

    [RelayCommand] private async Task CancelAsync() => await _navigationService.GoToAsync("..");
}