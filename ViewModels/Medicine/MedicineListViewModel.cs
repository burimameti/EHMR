using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class MedicineListViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly INavigationService _navigationService;
    private readonly ISelectedItemService<Medicine> _medicineSelectionService;

    public ObservableCollection<Medicine> Medicines { get; set; } = new();

    public MedicineListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        ISelectedItemService<Medicine> medicineSelectionService)
    {
        _dbFactory=dbFactory;
        _navigationService=navigationService;
        _medicineSelectionService=medicineSelectionService;

        _=LoadMedicinesAsync();
    }

    [RelayCommand]
    public async Task LoadMedicinesAsync()
    {
        Medicines.Clear();
        await using var db = await _dbFactory.CreateDbContextAsync();
        var list = await db.Set<Medicine>().AsNoTracking().OrderBy(m => m.Name).ToListAsync();
        foreach(var med in list) Medicines.Add(med);
    }

    [RelayCommand]
    private async Task NavigateToCreateAsync()
    {
        _medicineSelectionService.SelectedItem=null; // Сигнал за Креирање
        await _navigationService.GoToAsync(AppRoutes.Medicines.List);
    }

    [RelayCommand]
    private async Task NavigateToDetailsAsync(Medicine medicine)
    {
        if(medicine==null) return;
        _medicineSelectionService.SelectedItem=medicine; // Сигнал за Преглед/Измена
        await _navigationService.GoToAsync("medicinedetailformPage");
    }
}