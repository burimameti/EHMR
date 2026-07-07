using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class ProtocolRegistryViewModel : BaseViewModel<TherapyProtocol>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<TherapyProtocol> _selectedItemService;

    // Филтер за категории во твојот класичен стил со авто-апдејт
    private string _selectedCategory = "Сите";

    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if(SetProperty(ref _selectedCategory, value))
                ApplyPipeline();
        }
    }

    // Листа за филтрирање по медицински гранки (можеш да ја дополниш од база или статички)
    public ObservableCollection<string> CategoryFilters
    {
        get;
    } =
        new(["Сите", "Кардиологија", "Онкологија", "Нефрологија", "Пулмологија"]);

    /// <summary>Module key used by BaseViewModel&lt;T&gt;.EvaluatePermissions().</summary>
    protected override string ModuleName => "protocols";

    // ================= GRID (entity-specific, same shape as PatientListViewModel) =================
    [ObservableProperty] private ObservableCollection<TherapyProtocol> filteredProtocols = new();
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    public ProtocolRegistryViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<TherapyProtocol> selectedItemService,
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;

        EvaluatePermissions(); // base method — was never being called before, so CanCreate/Update/Delete stayed false
    }

    // LOAD DATA
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();

            var protocols = await db.TherapyProtocols
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            AllItems=protocols; // Полнење на заштитената база од твојот BaseViewModel

            InitializeSparkControls();

            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Неуспешно вчитување на протоколи: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // ФИЛТРИРАЊЕ НА ПРОТОКОЛИТЕ (Имплементација на апстрактниот метод од BaseViewModel)
    protected override IEnumerable<TherapyProtocol> ApplySearch(
        IEnumerable<TherapyProtocol> query,
        string search)
    {
        if(string.IsNullOrWhiteSpace(search))
            return query;

        return query.Where(x =>
            (x.Name?.Contains(search, StringComparison.OrdinalIgnoreCase)??false)||
            (x.Description?.Contains(search, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<TherapyProtocol> ApplyFilters(IEnumerable<TherapyProtocol> query)
    {
        if(SelectedCategory!="Сите")
            query=query.Where(x => x.DiseaseCategory==SelectedCategory);

        return query;
    }

    /// <summary>
    /// Mirrors PatientListViewModel.OnPageProjected: base pipeline calls this with
    /// the paged/sorted/filtered slice, we store it locally, and the partial
    /// OnFilteredProtocolsChanged hook below rebuilds the Spark grid rows.
    /// </summary>
    protected override void OnPageProjected(ObservableCollection<TherapyProtocol> page)
    {
        FilteredProtocols=page;
    }

    partial void OnFilteredProtocolsChanged(ObservableCollection<TherapyProtocol> value) => RefreshSparkGridRows();

    // ClearFilters command now comes from BaseViewModel<T> (ClearFiltersCommand):
    // it calls ResetFilters() → ApplyPipeline() → SyncSparkPickersFromFilters().
    protected override void ResetFilters()
    {
        SearchText=string.Empty;
        SelectedCategory="Сите";
    }

    // ============================================================
    // PICKERS — category filter now goes through the same
    // SparkPickerItem/MakePicker mechanism as Patients, instead of
    // a bare property, so it renders inside SparkExplorerHeaderView.
    // ============================================================
    private SparkPickerItem _categoryPicker;

    private void BuildSparkPickers()
    {
        Pickers.Clear();
        _categoryPicker=MakePicker("Категорија", CategoryFilters, SelectedCategory, s => SelectedCategory=s);
        Pickers.Add(_categoryPicker);
    }

    protected override void SyncSparkPickersFromFilters()
    {
        if(_categoryPicker==null) return;
        _categoryPicker.SelectedItem=SelectedCategory;
    }

    // ============================================================
    // GRID — columns/rows for TherapyProtocol
    // ============================================================
    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "НАЗИВ", Key = "Name", Width = new GridLength(2.2, GridUnitType.Star) },
            new() { Header = "КАТЕГОРИЈА", Key = "DiseaseCategory", CellType = SparkGridCellType.Badge, Width = new GridLength(1.3, GridUnitType.Star) },
            new() { Header = "ОПИС", Key = "Description", Width = new GridLength(2.8, GridUnitType.Star) },
            new() { Header = "ТРАЕЊЕ (ДЕНОВИ)", Key = "DurationInDays", CellType = SparkGridCellType.Number, Width = new GridLength(1.1, GridUnitType.Star) },
            new() { Header = "ИЗРАБОТИЛ", Key = "CreatedByDoctor", Width = new GridLength(1.4, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var p in FilteredProtocols)
        {
            var row = new SparkGridRow { Tag=p };
            row["Name"]=p.Name;
            row["DiseaseCategory"]=new SparkBadgeValue(p.DiseaseCategory??"—", CategoryToTone(p.DiseaseCategory));
            row["Description"]=p.Description;
            row["DurationInDays"]=p.DurationInDays??0;
            row["CreatedByDoctor"]=p.CreatedByDoctor??"—";
            rows.Add(row);
        }
        GridRows=rows;
    }

    /// <summary>
    /// Purely cosmetic grouping so the category badge has some visual variety —
    /// adjust freely, there's no clinical meaning behind the tone assignment.
    /// </summary>
    private static SparkBadgeTone CategoryToTone(string? category) => category?.ToLowerInvariant() switch
    {
        "онкологија" => SparkBadgeTone.Danger,
        "кардиологија" => SparkBadgeTone.Warning,
        "нефрологија" => SparkBadgeTone.Neutral,
        "пулмологија" => SparkBadgeTone.Success,
        _ => SparkBadgeTone.Neutral
    };

    // ============================================================
    // WIRING
    // ============================================================
    private void InitializeSparkControls()
    {
        BuildSparkPickers();
        BuildSparkButtons();   // inherited from BaseViewModel<T>
        BuildSparkGridColumns();
    }

    // ACTIONS

    [RelayCommand]
    private async Task AddAsync()
    {
        _selectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Protocols.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(TherapyProtocol protocol)
    {
        if(protocol==null) return;

        _selectedItemService.SelectedItem=protocol; // Праќаме постоечки -> ПРЕГЛЕД/ИЗМЕНА
        await NavigationService.GoToAsync(AppRoutes.Protocols.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(TherapyProtocol protocol)
    {
        if(protocol==null) return;

        _selectedItemService.SelectedItem=protocol;
        await NavigationService.GoToAsync(AppRoutes.Protocols.Detail);
    }

    [RelayCommand]
    private async Task DeleteAsync(TherapyProtocol protocol)
    {
        if(protocol==null) return;

        bool confirm = await UserDialogService.ShowConfirmationAsync(
            "Потврда за бришење",
            $"Дали сте сигурни дека сакате да го избришете шаблонот '{protocol.Name}'? Ова може да влијае на генерираните планови.");

        if(!confirm) return;

        try
        {
            IsBusy=true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            // Превентивна проверка: Пред бришење, Entity Framework може да ја повлече дупликат од контекст
            db.TherapyProtocols.Remove(protocol);
            await db.SaveChangesAsync();

            AllItems.Remove(protocol);
            ApplyPipeline();

            await UserDialogService.ShowAlertAsync("Успешно бришење", "Протоколот е отстранет од каталогот.", "OK");
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
}
