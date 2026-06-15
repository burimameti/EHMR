using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class ScheduleMedicineRuleDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<ScheduleMedicineRule> _ruleSelectionService;
    private readonly ISelectedItemService<TherapySchedule> _scheduleSelectionService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _userDialogService;

    [ObservableProperty] private ScheduleMedicineRule _rule = null!;
    [ObservableProperty] private List<Medicine> _medicinesList = [];
    [ObservableProperty] private Medicine? _selectedMedicine;
    [ObservableProperty] private string _pageTitle = "Конфигурација на прескрипционо правило";

    public ScheduleMedicineRuleDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<ScheduleMedicineRule> ruleSelectionService,
        ISelectedItemService<TherapySchedule> scheduleSelectionService,
        INavigationService navigationService,
        IUserDialogService userDialogService)
    {
        _dbFactory=dbFactory;
        _ruleSelectionService=ruleSelectionService;
        _scheduleSelectionService=scheduleSelectionService;
        _navigationService=navigationService;
        _userDialogService=userDialogService;

        _=LoadMedicinesAndInitAsync();
    }

    private async Task LoadMedicinesAndInitAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        MedicinesList=await db.Set<Medicine>().AsNoTracking().OrderBy(m => m.Name).ToListAsync();

        var currentRule = _ruleSelectionService.SelectedItem;
        var currentSchedule = _scheduleSelectionService.SelectedItem;

        if(currentRule==null)
        {
            // КРЕИРАЊЕ НА НОВО ПРАВИЛО ЗАКЛУЧЕНО ЗА ТЕКОВНИОТ РАСПОРЕД
            Rule=new ScheduleMedicineRule
            {
                TherapyScheduleId=currentSchedule?.Id??Guid.Empty
            };
            PageTitle="🧬 Нова Прескрипција за Протоколот";
            SelectedMedicine=MedicinesList.FirstOrDefault();
        }
        else
        {
            // ИЗМЕНА НА ПОСТОЕЧКО ПРАВИЛО
            Rule=currentRule;
            PageTitle=$"Прилагодување на: {Rule.Medicine.Name}";
            SelectedMedicine=MedicinesList.FirstOrDefault(m => m.Id==Rule.MedicineId);
        }
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if(SelectedMedicine==null)
        {
            await _userDialogService.ShowAlertAsync(
                "Грешка",
                "Изберете лек.",
                "OK");
            return;
        }

        if(string.IsNullOrWhiteSpace(Rule.Dosage))
        {
            await _userDialogService.ShowAlertAsync(
                "Грешка",
                "Внесете доза.",
                "OK");
            return;
        }

        if(Rule.Quantity<=0)
        {
            await _userDialogService.ShowAlertAsync(
                "Грешка",
                "Количината мора да биде поголема од 0.",
                "OK");
            return;
        }

        Rule.MedicineId=SelectedMedicine.Id;
        Rule.Medicine=SelectedMedicine;

        var schedule = _scheduleSelectionService.SelectedItem;

        if(schedule!=null&&
            !schedule.MedicineRules.Any(x => x.Id==Rule.Id))
        {
            schedule.MedicineRules.Add(Rule);
        }

        _ruleSelectionService.SelectedItem=null;

        await _navigationService.GoToAsync("..");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        _ruleSelectionService.SelectedItem=null;
        await _navigationService.GoToAsync("..");
    }
}