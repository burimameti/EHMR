using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class TherapyDetailsViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<TherapySchedule> _scheduleSelectionService;
    private readonly INavigationService _navigationService;
    private readonly IAuthStateService _authService;
    private readonly IUserDialogService _dialogService;

    [ObservableProperty] private TherapySchedule _schedule = null!;
    [ObservableProperty] private TreatmentPlan? _selectedTreatmentPlan;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditMode))]
    private bool _isReadOnly = false;

    public bool IsEditMode => !_isReadOnly;

    [ObservableProperty] private string _customProtocolName = string.Empty;
    [ObservableProperty] private string _customScheduleName = string.Empty;

    public ObservableCollection<ScheduleMedicineRule> DynamicMedicineRules { get; set; } = new();
    public ObservableCollection<TherapyCycle> PreviewCycles { get; set; } = new();

    [ObservableProperty] private List<Medicine> _availableMedicines = [];
    [ObservableProperty] private Medicine? _selectedMedicineForRule;
    [ObservableProperty] private string _dosageRuleText = string.Empty;
    [ObservableProperty] private int _administrationDayOffset = 0;

    private ScheduleMedicineRule? _ruleBeingEdited;
    [ObservableProperty] private string _addOrUpdateButtonText = "➕ Додај во План";
    [ObservableProperty] private int _numberOfCyclesToGenerate = 4;
    [ObservableProperty] private string _pageTitle = "Уредување на Протокол";

    // Управување со дијалогот за автоматски термини
    [ObservableProperty] private bool _isAppointmentPromptVisible = false;

    [ObservableProperty] private DateTime _autoAppointmentStartDate = DateTime.Today;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    public TherapyDetailsViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<TherapySchedule> scheduleSelectionService,
        INavigationService navigationService,
        IAuthStateService authService,
        IUserDialogService dialogService)
    {
        _dbFactory=dbFactory;
        _scheduleSelectionService=scheduleSelectionService;
        _navigationService=navigationService;
        _authService=authService;
        _dialogService=dialogService;
    }

    private bool CanEdit => !_isReadOnly;

    private void EnsureCanEdit()
    {
        if(_isReadOnly)
            return;
    }

    // =========================================================
    // ИНИЦИЈАЛИЗАЦИЈА (Рефакторирана со новиот безбедносен модел)
    // =========================================================
    public async Task InitializeAsync()
    {
        // РЕФАКТОРИРАНО: Наместо старите Perm, проверуваме дали корисникот ја има улогата/дозволата за Protocols модулот
        _isReadOnly=!_authService.Permissions.Contains(Domain.Entities.Modules.Protocols, StringComparer.OrdinalIgnoreCase);

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            AvailableMedicines=await db.Medicines.AsNoTracking().ToListAsync();

            var sharedSchedule = _scheduleSelectionService.SelectedItem;

            if(sharedSchedule==null||sharedSchedule.Id==Guid.Empty)
            {
                ResetForm();
            }
            else
            {
                Schedule=sharedSchedule;
                CustomScheduleName=Schedule.Name;

                var plan = await db.TreatmentPlans.AsNoTracking().FirstOrDefaultAsync(p => p.Id==Schedule.TreatmentPlanId);
                if(plan!=null)
                {
                    SelectedTreatmentPlan=plan;
                    CustomProtocolName=plan.ProtocolName;
                    PageTitle=$"Преглед на Протокол: {CustomProtocolName}";
                }

                DynamicMedicineRules.Clear();
                var rules = await db.ScheduleMedicineRules
                    .Include(r => r.Medicine)
                    .Where(r => r.TherapyScheduleId==Schedule.Id)
                    .AsNoTracking()
                    .ToListAsync();

                foreach(var r in rules) DynamicMedicineRules.Add(r);

                PreviewCycles.Clear();
                var cycles = await db.TherapyCycles
                    .Include(c => c.ScheduledDoses)
                    .ThenInclude(d => d.Medicine)
                    .Where(c => c.TherapyScheduleId==Schedule.Id)
                    .OrderBy(c => c.CycleNumber)
                    .AsNoTracking()
                    .ToListAsync();

                foreach(var c in cycles) PreviewCycles.Add(c);
            }
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка при вчитување", $"Неуспешно вчитување на терапевтските правила: {ex.Message}", "OK");
        }
    }

    // =========================================================
    // МЕНАЏИРАЊЕ НА ПРАВИЛА ЗА ЛЕКОВИ
    // =========================================================
    [RelayCommand]
    private void AddOrUpdateMedicineRule()
    {
        EnsureCanEdit();
        if(SelectedMedicineForRule==null||string.IsNullOrWhiteSpace(DosageRuleText)) return;

        if(_ruleBeingEdited!=null)
        {
            _ruleBeingEdited.MedicineId=SelectedMedicineForRule.Id;
            _ruleBeingEdited.Medicine=SelectedMedicineForRule;
            _ruleBeingEdited.Dosage=DosageRuleText;
            _ruleBeingEdited.AdministrationDayOffset=AdministrationDayOffset;

            var index = DynamicMedicineRules.IndexOf(_ruleBeingEdited);
            if(index>=0) DynamicMedicineRules[index]=_ruleBeingEdited;

            _ruleBeingEdited=null;
            AddOrUpdateButtonText="➕ Додај во План";
        }
        else
        {
            DynamicMedicineRules.Add(new ScheduleMedicineRule
            {
                Id=Guid.NewGuid(),
                TherapyScheduleId=Schedule.Id,
                MedicineId=SelectedMedicineForRule.Id,
                Medicine=SelectedMedicineForRule,
                Dosage=DosageRuleText,
                AdministrationDayOffset=AdministrationDayOffset,
                Quantity=1
            });
        }

        SelectedMedicineForRule=null;
        DosageRuleText=string.Empty;
        AdministrationDayOffset=0;

        if(PreviewCycles.Count>0) PreviewTherapyCycles();
    }

    [RelayCommand]
    private void PrepareRuleForEdit(ScheduleMedicineRule rule)
    {
        if(IsReadOnly||rule==null) return;
        _ruleBeingEdited=rule;
        SelectedMedicineForRule=AvailableMedicines.FirstOrDefault(m => m.Id==rule.MedicineId);
        DosageRuleText=rule.Dosage;
        AdministrationDayOffset=rule.AdministrationDayOffset;
        AddOrUpdateButtonText="💾 Ажурирај Ред";
    }

    [RelayCommand]
    private void RemoveMedicineRule(ScheduleMedicineRule rule)
    {
        if(IsReadOnly||rule==null) return;
        DynamicMedicineRules.Remove(rule);
        if(PreviewCycles.Count>0) PreviewTherapyCycles();
    }

    // =========================================================
    // СИМУЛАЦИЈА И ПРЕСМЕТКА НА ОДДЕЛНИ ЦИКЛУСИ
    // =========================================================
    [RelayCommand]
    private void PreviewTherapyCycles()
    {
        if(Schedule.FrequencyInDays<=0||NumberOfCyclesToGenerate<=0) return;

        // Чувај ја привремено старата состојба за да ги извлечеш статусите и ID-ата
        var oldCyclesMap = PreviewCycles.ToDictionary(c => c.CycleNumber, c => c);

        PreviewCycles.Clear();
        DateTime currentStartDate = Schedule.StartDate;

        for(int i = 1; i<=NumberOfCyclesToGenerate; i++)
        {
            DateTime currentEndDate = currentStartDate.AddDays(Schedule.FrequencyInDays-1);

            // Провери дали овој ред (циклус) веќе постоел претходно за да му го зачуваме статусот
            bool hasOldCycle = oldCyclesMap.TryGetValue(i, out var oldCycle);

            var generatedCycle = new TherapyCycle
            {
                // Ако постоел, задржи го истото ID за EF да знае дека е ист запис, во спротивно генерирај ново
                Id=hasOldCycle ? oldCycle!.Id : Guid.NewGuid(),
                TherapyScheduleId=Schedule.Id,
                CycleNumber=i,
                PlannedStartDate=currentStartDate,
                PlannedEndDate=currentEndDate,
                // АКО ПОСТОЕЛ ЗАДРЖИ ГО СТАТУСОТ (Completed/Active), во спротивно стави Planned
                Status=hasOldCycle ? oldCycle!.Status : TherapyStatus.Planned,
                ScheduledDoses=new List<CycleMedicationDose>()
            };

            foreach(var rule in DynamicMedicineRules)
            {
                decimal dosageValue = 0;
                string dosageUnit = string.Empty;

                if(!string.IsNullOrWhiteSpace(rule.Dosage))
                {
                    var parts = rule.Dosage.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if(parts.Length>0) decimal.TryParse(parts[0], out dosageValue);
                    if(parts.Length>1) dosageUnit=parts[1];
                }

                generatedCycle.ScheduledDoses.Add(new CycleMedicationDose
                {
                    Id=Guid.NewGuid(),
                    TherapyCycleId=generatedCycle.Id,
                    MedicineId=rule.MedicineId,
                    Medicine=rule.Medicine,
                    TargetDosage=rule.Dosage,
                    DosageValue=dosageValue,
                    DosageUnit=dosageUnit,
                    Quantity=rule.Quantity,
                    PlannedDate=generatedCycle.PlannedStartDate.AddDays(rule.AdministrationDayOffset),
                    PlannedAdministrationDate=generatedCycle.PlannedStartDate.AddDays(rule.AdministrationDayOffset),
                    Status=DoseStatus.Planned
                });
            }

            PreviewCycles.Add(generatedCycle);
            currentStartDate=currentEndDate.AddDays(1);
        }

        if(PreviewCycles.Count>0)
        {
            Schedule.EndDate=PreviewCycles.Last().PlannedEndDate;
        }
    }

    [RelayCommand]
    private void ResetForm()
    {
        _ruleBeingEdited=null;
        AddOrUpdateButtonText="➕ Додај во План";
        PageTitle="Креирање на Нов Терапевтски План";
        Schedule=new TherapySchedule { Id=Guid.NewGuid(), StartDate=DateTime.Today, FrequencyInDays=21, GraceDays=3, IsActive=true };
        CustomProtocolName=string.Empty;
        CustomScheduleName=string.Empty;
        DynamicMedicineRules.Clear();
        PreviewCycles.Clear();
        IsAppointmentPromptVisible=false;
    }

    // =========================================================
    // СИГУРНО ЗАЧУВУВАЊЕ ВО БАЗА
    // =========================================================
    [RelayCommand]
    private async Task SavePlanningAsync()
    {
        if(IsReadOnly)
        {
            await _dialogService.ShowAlertAsync("Одбиено", "Немате авторизација за конфигурирање медицински протоколи.", "OK");
            return;
        }

        if(string.IsNullOrWhiteSpace(CustomProtocolName)||PreviewCycles.Count==0)
        {
            await _dialogService.ShowAlertAsync("Внимание", "Внесете име на протокол и генерирајте ја временската оска со лекови!", "ОК");
            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            TreatmentPlan finalPlan;

            // 1. ПЕРЗИСТЕНЦИЈА НА TREATMENT PLAN
            if(SelectedTreatmentPlan!=null&&SelectedTreatmentPlan.Id!=Guid.Empty)
            {
                finalPlan=await db.TreatmentPlans.FirstAsync(p => p.Id==SelectedTreatmentPlan.Id);
                finalPlan.ProtocolName=CustomProtocolName;

                // Важно: Ако во интерфејсот имаш контрола за статус на самиот план, ажурирај ја тука:
                // finalPlan.Status = ...
            }
            else
            {
                finalPlan=new TreatmentPlan
                {
                    Id=Guid.NewGuid(),
                    ProtocolName=CustomProtocolName,
                    StartDate=Schedule.StartDate,
                    EndDate=Schedule.EndDate??DateTime.Today.AddMonths(6),
                    Status=TherapyStatus.Active,
                    PatientId=Guid.Empty // Ова подоцна ќе се врзе со реален PatientId
                };
                await db.TreatmentPlans.AddAsync(finalPlan);
            }

            // Зачувај го планот за да имаме валиден ID за надворешен клуч
            await db.SaveChangesAsync();

            // 2. ПОДГОТОВКА НА ТЕРАПЕВТСКИОТ РАСПОРЕД (SCHEDULE)
            Schedule.TreatmentPlanId=finalPlan.Id;
            Schedule.Name=CustomScheduleName;

            var isNewSchedule = !await db.TherapySchedules.AnyAsync(s => s.Id==Schedule.Id);
            if(isNewSchedule)
            {
                await db.TherapySchedules.AddAsync(Schedule);
                await db.SaveChangesAsync();
            }
            else
            {
                db.TherapySchedules.Update(Schedule);
            }

            // 3. ПАМЕТЕН ИЗБОР И АЖУРИРАЊЕ НА ПРАВИЛАТА ЗА ЛЕКОВИ (Medicine Rules)
            var dbRules = await db.ScheduleMedicineRules.Where(r => r.TherapyScheduleId==Schedule.Id).ToListAsync();
            db.ScheduleMedicineRules.RemoveRange(dbRules); // За правилата е во ред комплетен ресет бидејќи немаат статус

            foreach(var rule in DynamicMedicineRules)
            {
                rule.TherapyScheduleId=Schedule.Id;
                if(rule.Medicine!=null) db.Entry(rule.Medicine).State=EntityState.Unchanged;
                await db.ScheduleMedicineRules.AddAsync(rule);
            }

            // 4. СЛЕДЕЊЕ И ПЕРЗИСТИРАЊЕ НА ЦИКЛУСИТЕ (Therapy Cycles) БЕЗ ГУБЕЊЕ НА СТАТУСИ
            var existingDbCycles = await db.TherapyCycles
                .Include(c => c.ScheduledDoses)
                .Where(c => c.TherapyScheduleId==Schedule.Id)
                .ToDictionaryAsync(c => c.CycleNumber);

            foreach(var previewCycle in PreviewCycles)
            {
                previewCycle.TherapyScheduleId=Schedule.Id;

                if(existingDbCycles.TryGetValue(previewCycle.CycleNumber, out var dbCycle))
                {
                    // Постои во база: Ги ажурираме вредностите кои можеби се сменети, НО ГО ЧУВАМЕ СТАТУСОТ ако не е експлицитно сменет
                    dbCycle.PlannedStartDate=previewCycle.PlannedStartDate;
                    dbCycle.PlannedEndDate=previewCycle.PlannedEndDate;

                    // Преземи го статусот од меморискиот преглед (кој веќе го зачувавме во претходниот чекор)
                    dbCycle.Status=previewCycle.Status;
                    dbCycle.ReasonForMissing=previewCycle.ReasonForMissing;

                    // Избриши ги старите дози за овој конкретен циклус и додади ги новите според новите правила
                    if(dbCycle.ScheduledDoses!=null)
                    {
                        db.CycleMedicationDoses.RemoveRange(dbCycle.ScheduledDoses);
                    }

                    dbCycle.ScheduledDoses=new List<CycleMedicationDose>();
                    foreach(var dose in previewCycle.ScheduledDoses)
                    {
                        dose.TherapyCycleId=dbCycle.Id;
                        if(dose.Medicine!=null) db.Entry(dose.Medicine).State=EntityState.Unchanged;
                        dbCycle.ScheduledDoses.Add(dose);
                    }

                    db.TherapyCycles.Update(dbCycle);
                }
                else
                {
                    // Нов циклус (на пр. корисникот зголемил од 4 на 6 циклуси)
                    foreach(var dose in previewCycle.ScheduledDoses)
                    {
                        if(dose.Medicine!=null) db.Entry(dose.Medicine).State=EntityState.Unchanged;
                    }
                    await db.TherapyCycles.AddAsync(previewCycle);
                }
            }

            // Ако корисникот го НАМАЛИЛ бројот на циклуси (пр. од 4 на 2), избриши ги тие што фаќаат вишок
            var maxGeneratedCycleNumber = PreviewCycles.Max(c => c.CycleNumber);
            var excessCycles = existingDbCycles.Values.Where(c => c.CycleNumber>maxGeneratedCycleNumber);
            foreach(var excess in excessCycles)
            {
                if(excess.ScheduledDoses!=null) db.CycleMedicationDoses.RemoveRange(excess.ScheduledDoses);
                db.TherapyCycles.Remove(excess);
            }

            // 5. КОНЕЧНО СНИМАЊЕ НА СИТЕ ПРОМЕНИ ВО ЕДНА ТРАНСАКЦИЈА
            await db.SaveChangesAsync();

            // Ја ажурираме локалната состојба за автоматски термини
            AutoAppointmentStartDate=Schedule.StartDate;
            IsAppointmentPromptVisible=true;
        }
        catch(Exception ex)
        {
            var innerMsg = ex.InnerException!=null ? $"\nИнтерно: {ex.InnerException.Message}" : "";
            await _dialogService.ShowAlertAsync("Грешка при запис", $"{ex.Message}{innerMsg}", "ОК");
        }
    }

    // =========================================================
    // АВТОМАТСКА КАЛЕНДАРИЗАЦИЈА
    // =========================================================
    [RelayCommand]
    private async Task ConfirmAutoAppointments()
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            foreach(var cycle in PreviewCycles)
            {
                foreach(var dose in cycle.ScheduledDoses)
                {
                    int totalDaysOffset = (dose.PlannedDate-PreviewCycles[0].PlannedStartDate).Days;
                    DateTime targetAppointmentDate = AutoAppointmentStartDate.AddDays(totalDaysOffset);

                    var appointment = new Appointment
                    {
                        Id=Guid.NewGuid(),
                        PatientId=SelectedTreatmentPlan?.PatientId??Guid.Empty,
                        ScheduledStart=new DateTime(targetAppointmentDate.Year, targetAppointmentDate.Month, targetAppointmentDate.Day, 09, 00, 00),
                        ScheduledEnd=new DateTime(targetAppointmentDate.Year, targetAppointmentDate.Month, targetAppointmentDate.Day, 11, 00, 00),
                        Status=AppointmentStatus.Scheduled,
                        ReasonForVisit=$"Апликација на онколошка терапија: {dose.Medicine?.Name} ({dose.TargetDosage}) - Циклус бр. {cycle.CycleNumber}"
                    };

                    await db.Appointments.AddAsync(appointment);
                }
            }

            await db.SaveChangesAsync();

            IsAppointmentPromptVisible=false;
            await _dialogService.ShowAlertAsync("Календарот е ажуриран", "Успешно се креирани автоматски болнички термини за сите циклуси на лекови во медицинскиот систем!", "ОК");
            await _navigationService.GoToAsync(AppRoutes.Protocols.List);
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка календаризација", $"Неуспешно генерирање термини: {ex.Message}", "ОК");
        }
    }

    [RelayCommand]
    private async Task DeclineAutoAppointments()
    {
        IsAppointmentPromptVisible=false;
        await _navigationService.GoToAsync(AppRoutes.Protocols.List);
    }
}