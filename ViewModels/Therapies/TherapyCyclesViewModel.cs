using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class TherapyCyclesViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly IAuthStateService _authService;
    private readonly IUserDialogService _dialogService;
    private List<TherapyCycle> _allCycles = new();

    // Колекции за приказ во UI
    public ObservableCollection<TherapyCycle> FilteredCycles { get; set; } = new();

    public List<string> StatusOptions { get; set; } = Enum.GetNames(typeof(TherapyStatus)).ToList();

    // Безбедносни контроли за кориснички интерфејс
    [ObservableProperty] private bool _isReadOnly = false;

    // Селектиран циклус за едитирање

    // Селектиран циклус за едитирање
    private TherapyCycle? _selectedCycle;

    public TherapyCycle? SelectedCycle
    {
        get => _selectedCycle;
        set
        {
            if(SetProperty(ref _selectedCycle, value))
            {
                OnPropertyChanged(nameof(IsCycleSelected));
                LoadCycleToEditor(value);
            }
        }
    }

    public bool IsCycleSelected => SelectedCycle!=null;

    // Својства за уредување на формата (Двонасочен Binding)
    [ObservableProperty] private int _editCycleNumber;

    [ObservableProperty] private DateTime _editPlannedStartDate = DateTime.Now;
    [ObservableProperty] private DateTime _editPlannedEndDate = DateTime.Now.AddDays(7);
    [ObservableProperty] private string _editStatus;
    [ObservableProperty] private string _editReasonForMissing = string.Empty;

    // Својства за филтри во реално време
    private string _searchPatientText = string.Empty;

    public string SearchPatientText
    {
        get => _searchPatientText;
        set
        {
            if(SetProperty(ref _searchPatientText, value))
            {
                ApplyFilters();
            }
        }
    }

    private string? _selectedStatusFilter;

    public string? SelectedStatusFilter
    {
        get => _selectedStatusFilter;
        set
        {
            if(SetProperty(ref _selectedStatusFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================
    public TherapyCyclesViewModel(
       IDbContextFactory<DesktopTherapyDbContext> dbFactory,
       IAuthStateService authService,
       IUserDialogService dialogService)
    {
        _dbFactory=dbFactory;
        _authService=authService;
        _dialogService=dialogService;

        EvaluatePermissions();
    }

    private void EvaluatePermissions()
    {
        // Доколку ја нема оваа специфична пермисија, формата ќе биде заклучена
        IsReadOnly=!_authService.Permissions.Contains(Modules.Therapy, StringComparer.OrdinalIgnoreCase);
    }

    private void LoadCycleToEditor(TherapyCycle? cycle)
    {
        if(cycle==null) return;

        EditCycleNumber=cycle.CycleNumber;
        EditPlannedStartDate=cycle.PlannedStartDate;
        EditPlannedEndDate=cycle.PlannedEndDate;
        EditStatus=cycle.Status.ToString();
        EditReasonForMissing=cycle.ReasonForMissing??string.Empty;
    }

    private void ApplyFilters()
    {
        var result = _allCycles.AsEnumerable();

        if(!string.IsNullOrWhiteSpace(SearchPatientText))
        {
            result=result.Where(c => c.TherapySchedule?.TreatmentPlan?.Patient?.FullName?
                .Contains(SearchPatientText, StringComparison.OrdinalIgnoreCase)??false);
        }

        if(!string.IsNullOrWhiteSpace(SelectedStatusFilter))
        {
            result=result.Where(c => c.Status.ToString()==SelectedStatusFilter);
        }

        FilteredCycles.Clear();
        foreach(var cycle in result)
        {
            FilteredCycles.Add(cycle);
        }
    }

    // =========================================================
    // КОМАНДИ (Commands)
    // =========================================================

    /// <summary>
    /// Безбедно вчитување на податоци (Овој метод се повикува од екранот при OnAppearing)
    /// </summary>
    [RelayCommand]
    public async Task LoadDataAsync()
    {
        try
        {
            EvaluatePermissions();

            await using var db = await _dbFactory.CreateDbContextAsync();

            _allCycles=await db.TherapyCycles
                .Include(c => c.TherapySchedule)
                    .ThenInclude(s => s.TreatmentPlan)
                        .ThenInclude(tp => tp.Patient)
                .OrderByDescending(c => c.PlannedStartDate)
                .ToListAsync();

            ApplyFilters();
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync("Грешка", $"Неуспешно вчитување на циклусите: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task SaveCycleChangesAsync()
    {
        if(SelectedCycle==null)
            return;

        if(IsReadOnly)
        {
            await _dialogService.ShowAlertAsync(
                "Пристапот е одбиен",
                "Немате администраторски пермисии за рачна измена на активни терапевтски фази.",
                "OK");

            return;
        }

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var cycle = await db.TherapyCycles
                .FirstOrDefaultAsync(x => x.Id==SelectedCycle.Id);

            if(cycle==null)
            {
                await _dialogService.ShowAlertAsync(
                    "Грешка",
                    "Циклусот не е пронајден.",
                    "OK");

                return;
            }

            cycle.CycleNumber=EditCycleNumber;
            cycle.PlannedStartDate=EditPlannedStartDate;
            cycle.PlannedEndDate=EditPlannedEndDate;
            cycle.ReasonForMissing=EditReasonForMissing;

            if(Enum.TryParse<TherapyStatus>(EditStatus, out var parsedStatus))
            {
                cycle.Status=parsedStatus;
            }

            cycle.StatusChangedAt=DateTime.Now;

            await db.SaveChangesAsync();

            await LoadDataAsync();

            await _dialogService.ShowAlertAsync(
                "Успех",
                "Овој терапевтски циклус е успешно презапишан.",
                "OK");

            SelectedCycle=null;
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Грешка",
                ex.Message,
                "OK");
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        //  SearchText=string.Empty; // Доколку BaseViewModel содржи SearchText
        SearchPatientText=string.Empty;
        SelectedStatusFilter=null;
        ApplyFilters();
    }
}