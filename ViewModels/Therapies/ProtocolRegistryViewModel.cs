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

    // Филтер за категории
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

    public ObservableCollection<string> CategoryFilters
    {
        get;
    } =
        new(["Сите", "Кардиологија", "Онкологија", "Нефрологија", "Пулмологија"]);

    protected override string ModuleName => "protocols";
    protected override string DetailRoute => AppRoutes.Protocols.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за оваа акција со протоколи.";

    // ================= GRID =================
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
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;

        EvaluatePermissions();
    }

    // ============================================================
    // LOAD
    // ============================================================
    [RelayCommand]
    public async Task LoadAsync()
        => await ExecuteSafeAsync(LoadCoreAsync, "Неуспешно вчитување на протоколи");

    private async Task LoadCoreAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var protocols = await db.TherapyProtocols
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        AllItems=protocols;

        InitializeSparkControls();

        ApplyPipeline();
    }

    // ============================================================
    // PIPELINE HOOKS
    // ============================================================
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

    protected override void OnPageProjected(ObservableCollection<TherapyProtocol> page)
    {
        FilteredProtocols=page;
    }

    partial void OnFilteredProtocolsChanged(ObservableCollection<TherapyProtocol> value) => RefreshSparkGridRows();

    protected override void ResetFilters()
    {
        SearchText=string.Empty;
        SelectedCategory="Сите";
    }

    // ============================================================
    // PICKERS
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
    // GRID
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
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
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

            AddDefaultActions(p, row, detailLabel: "Преглед", editLabel: "✎");

            row["Actions"]=((List<SparkButtonItem>)row["Actions"])
                .Append(new SparkButtonItem
                {
                    Label="🗑",
                    Command=DeleteCommand,
                    CommandParameter=p,
                    IsPrimary=false,
                    IsEnabled=CanDelete
                })
                .ToList();

            rows.Add(row);
        }
        GridRows=rows;
    }

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
        BuildSparkButtons();
        BuildSparkGridColumns();
    }

    // ============================================================
    // DELETE (нема base еквивалент, останува локална команда)
    // ============================================================
    [RelayCommand]
    private async Task DeleteAsync(TherapyProtocol protocol)
    {
        if(protocol==null || !CanDelete) return;

        bool confirm = await UserDialogService.ShowConfirmationAsync(
            "Потврда за бришење",
            $"Дали сте сигурни дека сакате да го избришете шаблонот '{protocol.Name}'? Ова може да влијае на генерираните планови.");

        if(!confirm) return;

        await ExecuteSafeAsync(async () =>
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            db.TherapyProtocols.Remove(protocol);
            await db.SaveChangesAsync();

            AllItems.Remove(protocol);
            ApplyPipeline();

            await UserDialogService.ShowAlertAsync("Успешно бришење", "Протоколот е отстранет од каталогот.", "OK");
        }, "Грешка при бришење");
    }
}