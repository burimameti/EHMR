using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.ViewModels.Doctors.Extensions;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class DoctorsListViewModel : BaseViewModel<Doctor>, IQueryAttributable
{
    // ================= SERVICES =================
    private readonly IDoctorService _doctorService;

    // ================= FILTER STATE =================
    [ObservableProperty] private string selectedStatus = "All";
    [ObservableProperty] private string selectedSpecialty = "All";
    [ObservableProperty] private string filteredDoctorsCount = "";
    [ObservableProperty] private ObservableCollection<Doctor> filteredDoctors = new();

    // ================= QUERY STATE (deep-link support) =================
    private string? _pendingSearch;
    private string? _pendingStatus;

    // ================= BASE OVERRIDES =================
    protected override string ModuleName => Modules.Doctors;
    protected override string DetailRoute => AppRoutes.Doctors.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нов лекар.";

    public ObservableCollection<string> StatusFilters { get; } = DoctorFilterLookups.Status.ToObservableCollection();
    public ObservableCollection<string> SpecialtyFilters { get; } = DoctorFilterLookups.Specialty.ToObservableCollection();

    public string SelectedStatusDisplay
    {
        get => DoctorFilterLookups.Status.ToDisplay(SelectedStatus);
        set
        {
            var internalValue = DoctorFilterLookups.Status.ToInternal(value);
            if(SelectedStatus==internalValue) return;
            SelectedStatus=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    public string SelectedSpecialtyDisplay
    {
        get => DoctorFilterLookups.Specialty.ToDisplay(SelectedSpecialty);
        set
        {
            var internalValue = DoctorFilterLookups.Specialty.ToInternal(value);
            if(SelectedSpecialty==internalValue) return;
            SelectedSpecialty=internalValue;
            ApplyPipeline();
            OnPropertyChanged();
        }
    }

    /// <summary>Alias kept for XAML compatibility — forwards to the base class's SearchText.</summary>
    public string DoctorSearchText
    {
        get => SearchText;
        set => SearchText=value;
    }

    public ICommand SearchCommand
    {
        get;
    }

    // ================= CTOR =================
    public DoctorsListViewModel(
        IDoctorService doctorService,
        ISelectedItemService<Doctor> selectedItemService,
        INavigationService navigationService,
        IUserDialogService dialog,
        IMenuService menu,
        IAuthorizationService authorization)
        : base(navigationService, dialog, menu, authorization, selectedItemService)
    {
        _doctorService=doctorService;
        PageSize=10;

        SearchCommand=new Command<string>(query => SearchText=query);

        PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SearchText))
                OnPropertyChanged(nameof(DoctorSearchText));
        };

        EvaluatePermissions();
    }

    // ================= LOAD =================
    [RelayCommand]
    public async Task LoadAsync()
    {
        var data = await _doctorService.GetAllAsync();
        AllItems=data.ToList();

        InitializeSparkControls();

        if(!string.IsNullOrWhiteSpace(_pendingSearch)) SearchText=_pendingSearch;
        if(!string.IsNullOrWhiteSpace(_pendingStatus)) SelectedStatus=_pendingStatus;
        _pendingSearch=null;
        _pendingStatus=null;

        ApplyPipeline();
    }

    // ================= SEARCH =================
    protected override IEnumerable<Doctor> ApplySearch(IEnumerable<Doctor> query, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return query;

        var s = search.Trim();
        return query.Where(x =>
            (!string.IsNullOrWhiteSpace(x.FullName)&&x.FullName.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.Specialty)&&x.Specialty.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.LicenseNumber)&&x.LicenseNumber.Contains(s, StringComparison.OrdinalIgnoreCase))||
            (!string.IsNullOrWhiteSpace(x.ContactPhone)&&x.ContactPhone.Contains(s, StringComparison.OrdinalIgnoreCase))
        );
    }

    // ================= FILTER =================
    protected override IEnumerable<Doctor> ApplyFilters(IEnumerable<Doctor> query)
    {
        if(SelectedStatus=="Active")
            query=query.Where(x => x.IsActive);
        else if(SelectedStatus=="Inactive")
            query=query.Where(x => !x.IsActive);

        if(SelectedSpecialty!="All")
            query=query.Where(x => x.Specialty==SelectedSpecialty);

        return query;
    }

    // ================= SORT =================
    protected override IEnumerable<Doctor> ApplySort(IEnumerable<Doctor> query)
        => query.OrderBy(x => x.FullName);

    // ================= PAGE RESULT =================
    protected override void OnPageProjected(ObservableCollection<Doctor> page)
    {
        FilteredDoctors=page;
        FilteredDoctorsCount=$"{TotalItems} резултати";
    }

    // ================= RESET =================
    protected override void ResetFilters()
    {
        SelectedStatus="All";
        SelectedSpecialty="All";
        SearchText="";

        OnPropertyChanged(nameof(SelectedStatusDisplay));
        OnPropertyChanged(nameof(SelectedSpecialtyDisplay));
    }

    // ================= QUERY =================
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _pendingSearch=query.TryGetValue("search", out var s) ? s?.ToString() : null;
        _pendingStatus=query.TryGetValue("statusFilter", out var f) ? f?.ToString() : null;
    }

    // ================= PROPERTY CHANGES =================
    partial void OnSelectedStatusChanged(string value)
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
            tab.IsSelected=internalValue==value;
    }

    // ============================================================
    // TABS
    // ============================================================
    private readonly Dictionary<string, SparkTabItem> _statusTabsByInternal = new();

    private void BuildSparkTabs()
    {
        Tabs.Clear();
        _statusTabsByInternal.Clear();

        foreach(var display in StatusFilters)
        {
            var internalValue = DoctorFilterLookups.Status.ToInternal(display);

            var tab = new SparkTabItem
            {
                Title=display,
                IsSelected=SelectedStatus==internalValue
            };

            tab.Command=new RelayCommand(() => SelectTab(tab, () => SelectedStatusDisplay=display));

            Tabs.Add(tab);
            _statusTabsByInternal[internalValue]=tab;
        }

        RefreshSparkTabCounts();
    }

    private void RefreshSparkTabCounts()
    {
        foreach(var (internalValue, tab) in _statusTabsByInternal)
        {
            tab.Value=internalValue switch
            {
                "All" => AllItems.Count.ToString("N0"),
                "Active" => AllItems.Count(d => d.IsActive).ToString("N0"),
                "Inactive" => AllItems.Count(d => !d.IsActive).ToString("N0"),
                _ => "0"
            };
        }
    }

    // ============================================================
    // PICKERS
    // ============================================================
    private SparkPickerItem _specialtyPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _specialtyPicker=MakePicker("Специјалност", SpecialtyFilters, SelectedSpecialtyDisplay, s => SelectedSpecialtyDisplay=s);
        Pickers.Add(_specialtyPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_specialtyPicker==null) return;
        _specialtyPicker.SelectedItem=SelectedSpecialtyDisplay;
    }

    // ============================================================
    // GRID
    // ============================================================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
                  new() { Header = "БРОЈ", Key = "DoctorNumber", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ЛЕКАР", Key = "FullName", Width = new GridLength(2.2, GridUnitType.Star) },
            new() { Header = "Е-ПОШТА", Key = "Email", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "СПЕЦИЈАЛНОСТ", Key = "Specialty", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "ЛИЦЕНЦА", Key = "LicenseNumber", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "ТЕЛЕФОН", Key = "ContactPhone", Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    partial void OnFilteredDoctorsChanged(ObservableCollection<Doctor> value) => RefreshSparkGridRows();

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var d in FilteredDoctors)
        {
            var row = new SparkGridRow { Tag=d };
            row["DoctorNumber"]=d.DoctorNumber;
            row["FullName"]=d.FullName;
            row["Email"]=d.Email??"email@klinika.com"; // TODO: додади Email во база
            row["Specialty"]=d.Specialty;
            row["LicenseNumber"]=d.LicenseNumber;
            row["ContactPhone"]=d.ContactPhone;
            row["Status"]=new SparkBadgeValue(
                d.IsActive ? "Активен" : "Неактивен",
                d.IsActive ? SparkBadgeTone.Success : SparkBadgeTone.Danger);

            AddDefaultActions(d, row, detailLabel: "Детали", editLabel: "Промени");

            rows.Add(row);
        }
        GridRows=rows;
    }

    // ============================================================
    // WIRING
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkTabs();
        BuildSparkPickers();
        BuildSparkButtons();
        BuildSparkGridColumns();
    }
}