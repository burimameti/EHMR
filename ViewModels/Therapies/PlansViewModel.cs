using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class PlansViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly INavigationService _navigationService;
    private readonly ISelectedItemService<TherapySchedule> _scheduleSelectionService;
    private readonly IUserDialogService _userDialogService;

    public ObservableCollection<TreatmentPlan> AllRegisteredPlans { get; set; } = new();

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private TreatmentPlan? _selectedPlanInGrid;
    [ObservableProperty] private Guid _selectedProtocolId;

    public PlansViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        ISelectedItemService<TherapySchedule> scheduleSelectionService,
        IUserDialogService userDialogService)
    {
        _dbFactory=dbFactory;
        _navigationService=navigationService;
        _scheduleSelectionService=scheduleSelectionService;
        _userDialogService=userDialogService;
    }

    /// <summary>
    /// Се повикува автоматски секој пат кога екранот се појавува на таблетот/компјутерот
    /// </summary>
    [RelayCommand]
    public async Task LoadPlansAsync()
    {
        if(IsLoading) return;
        IsLoading=true;
        AllRegisteredPlans.Clear();

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            List<TreatmentPlan> plans;
            if(SelectedProtocolId!=Guid.Empty)
            {
                plans=await db.TreatmentPlans
              .Include(p => p.Patient).Include(p => p.Doctor)
              .Include(p => p.TherapySchedules)

                  .ThenInclude(s => s.MedicineRules)
                .Include(p => p.TherapyProtocol).Where(p => p.TherapyProtocolId==SelectedProtocolId)
              .OrderByDescending(p => p.StartDate)
              .AsNoTracking()
              .ToListAsync();
            }
            else
            {
                plans=await db.TreatmentPlans
                  .Include(p => p.Patient).Include(p => p.Doctor)
                  .Include(p => p.TherapySchedules)

                      .ThenInclude(s => s.MedicineRules)
                    .Include(p => p.TherapyProtocol)
                  .OrderByDescending(p => p.StartDate)
                  .AsNoTracking()
                  .ToListAsync();
            }

            foreach(var plan in plans)
            {
                AllRegisteredPlans.Add(plan);
            }
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка при вчитување", ex.Message, "ОК");
        }
        finally
        {
            IsLoading=false;
        }
    }

    [RelayCommand]
    private async Task CreateNewPlanAsync()
    {
        _scheduleSelectionService.SelectedItem=null;
        await _navigationService.GoToAsync(AppRoutes.Plans.Details);
    }

    [RelayCommand]
    private async Task EditPlanAsync(TreatmentPlan plan)
    {
        if(plan==null) return;

        try
        {
            IsLoading=true;

            // КЛУЧЕН ФИКС: Мораме да ги вчитаме и MedicineRules од базата со целосен граф,
            // во спротивно уредувањето ќе ги изгуби правилата за лекови!
            await using var db = await _dbFactory.CreateDbContextAsync();

            var scheduleWithRules = await db.TherapySchedules
                .Include(s => s.MedicineRules)
                    .ThenInclude(r => r.Medicine)
                .Include(s => s.Cycles)
                    .ThenInclude(c => c.ScheduledDoses)
                .FirstOrDefaultAsync(s => s.TreatmentPlanId==plan.Id);

            if(scheduleWithRules==null)
            {
                // Ако нема распоред, правиме нов објект
                scheduleWithRules=new TherapySchedule
                {
                    TreatmentPlanId=plan.Id,
                    Id=Guid.NewGuid(),
                    StartDate=plan.StartDate
                };
            }

            // Го зачувуваме комплетниот објект во сервисот за споделување
            _scheduleSelectionService.SelectedItem=scheduleWithRules;

            // Навигирај до екранот за уредување (Провери дали името на рутата се поклопува)
            await _navigationService.GoToAsync(AppRoutes.Therapy.Details);
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка", $"Не успеа вчитувањето на планот за измена: {ex.Message}", "ОК");
        }
        finally
        {
            IsLoading=false;
        }
    }

    [RelayCommand]
    private async Task DeletePlanAsync(TreatmentPlan plan)
    {
        if(plan==null) return;

        bool confirm = await _userDialogService.ShowConfirmationAsync(
            "Внимание!",
            $"Дали сте сигурни дека сакате комплетно да го избришете планот '{plan.ProtocolName}'?\n\nОва дејство е трајно и ќе ги избрише сите генерирани дози!",
            "Да, Избриши",
            "Откажи");

        if(!confirm) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var planToDelete = await db.TreatmentPlans.FirstOrDefaultAsync(p => p.Id==plan.Id);
            if(planToDelete!=null)
            {
                db.TreatmentPlans.Remove(planToDelete);
                await db.SaveChangesAsync();

                AllRegisteredPlans.Remove(plan);
                await _userDialogService.ShowAlertAsync("Успешно", "Планот е трајно отстранет од базата.", "ОК");
                await _navigationService.GoToAsync(AppRoutes.Plans.List);
            }
        }
        catch(Exception ex)
        {
            await _userDialogService.ShowAlertAsync("Грешка при бришење", ex.Message, "ОК");
        }
    }
}