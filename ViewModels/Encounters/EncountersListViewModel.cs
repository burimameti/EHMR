using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels.Constants;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class EncounterListViewModel : BaseViewModel<Encounter>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IAuthorizationService _authorization;

    private string _pendingSearch = string.Empty;
    private string _pendingStatus = string.Empty;

    // =====================================================
    // BACKING FIELDS FOR INTERNAL KEYS
    // =====================================================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedPriority = "All";
    [ObservableProperty] private string selectedEncounterType = "All";
    [ObservableProperty] private string selectedDoctor = "All";
 

    [ObservableProperty] private bool canCreateEncounter;
    [ObservableProperty] private bool canUpdateEncounter;
    [ObservableProperty] private bool canDeleteEncounter;

    // =====================================================
    // CACHED UI LOOKUPS
    // =====================================================
    public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>(new[] { "Сите" }.Concat(EncounterStatusSchema.Display.Values.ToObservableCollection()));
    public ObservableCollection<string> PriorityFilters { get; } = new ObservableCollection<string>(new[] { "Сите" }.Concat(EncounterPrioritySchema.Display.Values.ToObservableCollection()));
    public ObservableCollection<string> EncounterTypeFilters { get; } = new ObservableCollection<string>(new[] { "Сите" }.Concat(EncounterTypeSchema.Display.Values));

    // =====================================================
    // DISPLAY PROPERTIES (BIND THESE IN XAML LIKE PATIENTS)
    // =====================================================

    // 1. Plain private backing fields (No attributes)
    private bool _filterByDate;
    private DateTime _filterDate = DateTime.Today;

    // 2. Manual Property for the Checkbox/Toggle
    public bool FilterByDate
    {
        get => _filterByDate;
        set
        {
            if(SetProperty(ref _filterByDate, value))
            {
                CurrentPage=1;
                ApplyPipeline();
                RefreshStatistics();
            }
        }
    }

    // 3. Manual Property for the Date Picker
    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(SetProperty(ref _filterDate, value))
            {
                // Only update the list if the user actually wants to filter by date
                if(FilterByDate)
                {
                    CurrentPage=1;
                    ApplyPipeline();
                    RefreshStatistics();
                }
            }
        }
    }
    public string SelectedStatusDisplay
    {
        get => EncounterStatusSchema.ToDisplay(SelectedStatus);
        set
        {
            var internalValue = EncounterStatusSchema.ToKeyFromDisplay(value);
            if(SelectedStatus==internalValue) return;

            SelectedStatus=internalValue;
            CurrentPage=1;
            ApplyPipeline();
            RefreshStatistics();
            OnPropertyChanged();
        }
    }

    public string SelectedPriorityDisplay
    {
        get => EncounterPrioritySchema.ToDisplay(SelectedPriority);
        set
        {
            var internalValue = EncounterPrioritySchema.ToKeyFromDisplay(value);
            if(SelectedPriority==internalValue) return;

            SelectedPriority=internalValue;
            CurrentPage=1;
            ApplyPipeline();
            RefreshStatistics();
            OnPropertyChanged();
        }
    }

    public string SelectedEncounterTypeDisplay
    {
        get => EncounterTypeSchema.ToDisplay(SelectedEncounterType);
        set
        {
            var internalValue = EncounterTypeSchema.ToKeyFromDisplay(value);
            if(SelectedEncounterType==internalValue) return;

            SelectedEncounterType=internalValue;
            CurrentPage=1;
            ApplyPipeline();
            RefreshStatistics();
            OnPropertyChanged();
        }
    }

    // =====================================================
    // STATS
    // =====================================================
    [ObservableProperty] private int totalEncounters;
    [ObservableProperty] private int waitingCount;
    [ObservableProperty] private int inProgressCount;
    [ObservableProperty] private int completedCount;
    [ObservableProperty] private int cancelledCount;

    public EncounterListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<Encounter> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _authorization=authService;
        _selectedItemService=selectedItemService;

        EvaluatePermissions();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString()??string.Empty : string.Empty;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString()??string.Empty : string.Empty;
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

            var loadedItems = await db.Encounters
                .AsNoTracking()
                .Include(x => x.Patient)
                .Include(x => x.Doctor).ThenInclude(d => d.User)
                .Include(x => x.Appointment)
                .OrderByDescending(x => x.ScheduledStart??x.EncounterDate)
                .ToListAsync();

            AllItems=loadedItems;
            ApplyPendingQuery();
            ApplyPipeline();
            RefreshStatistics();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private void ApplyPendingQuery()
    {
        if(!string.IsNullOrWhiteSpace(_pendingSearch))
            SearchText=_pendingSearch;

        if(!string.IsNullOrWhiteSpace(_pendingStatus))
            SelectedStatus=_pendingStatus;

        CurrentPage=1;
        _pendingSearch=string.Empty;
        _pendingStatus=string.Empty;
    }

    // =====================================================
    // EXPLICIT FILTER CHANGE TRIGGERS (FOR MANUAL PICKER ACTIONS)
    // =====================================================
    [RelayCommand]
    private void SelectedStatusChanged(string? value)
    {
        SelectedStatus=EncounterStatusSchema.ToKeyFromDisplay(value??string.Empty);
        CurrentPage=1;
        ApplyPipeline();
        RefreshStatistics();
    }

    [RelayCommand]
    private void SelectedPriorityChanged(string? value)
    {
        SelectedPriority=EncounterPrioritySchema.ToKeyFromDisplay(value??string.Empty);
        CurrentPage=1;
        ApplyPipeline();
        RefreshStatistics();
    }

    [RelayCommand]
    private void SelectedEncounterTypeChanged(string? value)
    {
        SelectedEncounterType=EncounterTypeSchema.ToKeyFromDisplay(value??string.Empty);
        CurrentPage=1;
        ApplyPipeline();
        RefreshStatistics();
    }



    protected override IEnumerable<Encounter> ApplyFilters(IEnumerable<Encounter> query)
    {
        if(SelectedStatus!="All")
            query=query.Where(x => string.Equals(x.Status.ToString(), SelectedStatus, StringComparison.OrdinalIgnoreCase));

        if(SelectedPriority!="All")
            query=query.Where(x => string.Equals(x.Priority, SelectedPriority, StringComparison.OrdinalIgnoreCase));

        if(SelectedEncounterType!="All")
            query=query.Where(x => string.Equals(x.EncounterType, SelectedEncounterType, StringComparison.OrdinalIgnoreCase));

        if(SelectedDoctor!="All"&&!string.IsNullOrWhiteSpace(SelectedDoctor))
        {
            query=query.Where(x => x.Doctor!=null&&
                (x.Doctor.User.FirstName+" "+x.Doctor.User.LastName)
                .Contains(SelectedDoctor, StringComparison.OrdinalIgnoreCase));
        }

        if(FilterByDate)
        {
            query=query.Where(x => (x.ScheduledStart??x.EncounterDate).Date==FilterDate.Date);
        }

        if(!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim();
            query=query.Where(x =>
                (x.EncounterNumber??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
                (x.Patient!=null&&(x.Patient.FirstName+" "+x.Patient.LastName).Contains(term, StringComparison.OrdinalIgnoreCase))||
                (x.Doctor!=null&&(x.Doctor.User.FirstName+" "+x.Doctor.User.LastName).Contains(term, StringComparison.OrdinalIgnoreCase))||
                (x.ChiefComplaint??"").Contains(term, StringComparison.OrdinalIgnoreCase)||
                (x.ReasonForVisit??"").Contains(term, StringComparison.OrdinalIgnoreCase)
            );
        }

        return query.OrderByDescending(x => x.ScheduledStart??x.EncounterDate);
    }

    private void RefreshStatistics()
    {
        var data = AllItems;
        if(data==null) return;

        TotalEncounters=data.Count;
        WaitingCount=data.Count(x => x.Status==EncounterStatus.Scheduled);
        InProgressCount=data.Count(x => x.Status==EncounterStatus.InProgress);
        CompletedCount=data.Count(x => x.Status==EncounterStatus.Completed);
        CancelledCount=data.Count(x => x.Status==EncounterStatus.Cancelled);
    }

    [RelayCommand] private async Task NewEncounter() => await NavigationService.GoToAsync(AppRoutes.Encounters.Create);

    [RelayCommand]
    private async Task OpenEncounter(Encounter? encounter)
    {
        if(encounter==null) return;
        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Detail);
    }

    [RelayCommand]
    private async Task EditEncounter(Encounter? encounter)
    {
        if(encounter==null) return;
        _selectedItemService.SelectedItem=encounter;
        await NavigationService.GoToAsync(AppRoutes.Encounters.Edit);
    }

    private bool _isClearingFilters = false;
    [RelayCommand]
    private void ClearFilters()
    {
        // If we are already clearing, ignore any accidental cascading UI triggers
        if(_isClearingFilters) return;

        try
        {
            _isClearingFilters=true;

            // 1. Reset all backing fields silently
            SearchText=string.Empty;
            CurrentPage=1;

            SelectedStatus="All";
            SelectedPriority="All";
            SelectedEncounterType="All";
            SelectedDoctor="All";

            FilterByDate =false;
            FilterDate=DateTime.Today;

            // 2. Notify UI bindings in a single batch
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(FilterByDate));
            OnPropertyChanged(nameof(FilterDate));
            OnPropertyChanged(nameof(SelectedStatusDisplay));
            OnPropertyChanged(nameof(SelectedPriorityDisplay));
            OnPropertyChanged(nameof(SelectedEncounterTypeDisplay));

            // 3. Process data exactly once
            ApplyPipeline();
           CurrentPage=1;
            RefreshStatistics();
        }
        finally
        {
            _isClearingFilters=false;
        }
    }
    private void EvaluatePermissions()
    {
        var hasModule = _authorization.CanAccessModule("encounters");
        CanCreateEncounter=hasModule;
        CanUpdateEncounter=hasModule;
        CanDeleteEncounter=hasModule;
    }
}