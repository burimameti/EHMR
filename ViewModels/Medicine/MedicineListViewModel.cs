using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Resources.Controls;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class MedicineListViewModel : BaseViewModel<Medicine>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    protected override string ModuleName => Modules.Inventory;
    protected override string DetailRoute => AppRoutes.Medicines.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нов медикамент.";

    public ICommand SearchCommand
    {
        get;
    }

    [ObservableProperty] private ObservableCollection<Medicine> filteredMedicines = new();

    public MedicineListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<Medicine> selectedItemService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _dbFactory=dbFactory;

        PageSize=10;
        SearchCommand=new Command<string>(query => SearchText=query);

        EvaluatePermissions();
        BuildSparkGridColumns();
        BuildSparkButtons();
    }
   
    // ================= LOAD =================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;
        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();
            var list = await db.Set<Medicine>()
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync();

            AllItems=list;
            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување медикаменти: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // ================= SEARCH =================
    protected override IEnumerable<Medicine> ApplySearch(IEnumerable<Medicine> items, string search)
    {
        if(string.IsNullOrWhiteSpace(search)) return items;

        var s = search.Trim();
        return items.Where(x =>
            (x.Name?.Contains(s, StringComparison.OrdinalIgnoreCase)??false)||
            (x.GenericName?.Contains(s, StringComparison.OrdinalIgnoreCase)??false)||
            (x.DosageForm?.Contains(s, StringComparison.OrdinalIgnoreCase)??false));
    }

    protected override IEnumerable<Medicine> ApplyFilters(IEnumerable<Medicine> query) => query;

    protected override IEnumerable<Medicine> ApplySort(IEnumerable<Medicine> query) =>
        query.OrderBy(x => x.Name);

    protected override void ResetFilters() => SearchText=string.Empty;

    protected override void OnPageProjected(ObservableCollection<Medicine> page)
    {
        FilteredMedicines=page;
        RefreshSparkGridRows();
    }

    // ================= SPARK GRID =================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "НАЗИВ", Key = "Name", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ГЕНЕРИЧКО ИМЕ", Key = "GenericName", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ФОРМА", Key = "DosageForm", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "СТАНДАРДНА ДОЗА", Key = "DefaultDosage", Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "ОПЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    private void RefreshSparkGridRows()
    {
        var rows = new ObservableCollection<SparkGridRow>();
        foreach(var m in FilteredMedicines)
        {
            var row = new SparkGridRow { Tag=m };
            row["Name"]=m.Name;
            row["GenericName"]=m.GenericName;
            row["DosageForm"]=m.DosageForm;
            row["DefaultDosage"]=m.DefaultDosage;
            AddDefaultActions(m, row);
            rows.Add(row);
        }
        GridRows=rows;
    }
}