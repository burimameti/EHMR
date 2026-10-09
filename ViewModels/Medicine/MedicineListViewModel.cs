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

    protected override string ModuleName => Modules.Medicines;
    protected override string DetailRoute => AppRoutes.Medicines.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нов медикамент.";

    [RelayCommand]
    private async Task NewMedicineAsync()
    {
        SelectedItemService.SelectedItem=default;
        SelectedItemService.OpenInEditMode=true;
        await NavigationService.GoToAsync(DetailRoute);
    }

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
            await LoadCatalogsAsync();
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
    [ObservableProperty] private ObservableCollection<ApplicationRegime> applicationRegimes = new();
    [ObservableProperty] private ObservableCollection<SparkGridColumn> applicationRegimeColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> applicationRegimeRows = new();

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

    [RelayCommand]
    private async Task DeleteMedicineAsync(Medicine? medicine)
    {
        if(!CanDelete || medicine is null)
            return;

        var confirmed = await UserDialogService.ShowConfirmationAsync(
            "Избриши лек",
            $"Дали сте сигурни дека сакате да го избришете лекот „{medicine.Name}“?",
            "Да",
            "Не");

        if(!confirmed)
            return;

        try
        {
            IsBusy=true;
            await using var db = await _dbFactory.CreateDbContextAsync();

            var entity = await db.Set<Medicine>().FirstOrDefaultAsync(x => x.Id == medicine.Id);
            if(entity is null)
                return;

            db.Set<Medicine>().Remove(entity);
            await db.SaveChangesAsync();

            AllItems.RemoveAll(x => x.Id == medicine.Id);
            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при бришење на лекот: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    private async Task LoadCatalogsAsync()
    {
        await LoadApplicationRegimesAsync();
    }

    private async Task LoadApplicationRegimesAsync()
    {
        await using var db=await _dbFactory.CreateDbContextAsync();
        ApplicationRegimes=new ObservableCollection<ApplicationRegime>(await db.Set<ApplicationRegime>().AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Regime).ToListAsync());
        ApplicationRegimeColumns=new ObservableCollection<SparkGridColumn>
        {
            new(){Header="РЕЖИМ НА АПЛИКАЦИЈА",Key="Regime",Width=new GridLength(3,GridUnitType.Star)},
            new(){Header="ОПЦИИ",Key="Actions",CellType=SparkGridCellType.Actions,Width=GridLength.Auto}
        };
        var rows=new ObservableCollection<SparkGridRow>();
        foreach(var item in ApplicationRegimes)
        {
            var row=new SparkGridRow{Tag=item}; row["Regime"]=item.Regime;
            row["Actions"]=new List<SparkButtonItem>
            {
                new(){IconGlyph="\\uf06e",Label="Преглед",Command=PreviewApplicationRegimeCommand,CommandParameter=item,IsEnabled=CanView},
                new(){IconGlyph="\\uf044",Label="Уреди",Command=EditApplicationRegimeCommand,CommandParameter=item,IsEnabled=CanUpdate},
                new(){IconGlyph="\\uf1f8",Label="Избриши",Command=DeleteApplicationRegimeCommand,CommandParameter=item,IsEnabled=CanDelete}
            };
            rows.Add(row);
        }
        ApplicationRegimeRows=rows;
    }

    [RelayCommand]
    private async Task AddApplicationRegimeAsync()
    {
        var value=await UserDialogService.ShowPromptAsync("Нов режим на апликација","Внесете режим на апликација.","Додај","Откажи","Пример: Поткожно");
        if(string.IsNullOrWhiteSpace(value)) return;
        value=value.Trim();
        await using var db=await _dbFactory.CreateDbContextAsync();
        if(await db.Set<ApplicationRegime>().AnyAsync(x=>x.IsActive&&x.Regime.ToLower()==value.ToLower()))
        {
            await UserDialogService.ShowAlertAsync("Постои запис",$"Режимот „{value}“ веќе постои.","ОК"); return;
        }
        db.Set<ApplicationRegime>().Add(new ApplicationRegime{Id=Guid.NewGuid(),Regime=value,IsActive=true});
        await db.SaveChangesAsync();
        await LoadApplicationRegimesAsync();
    }

    [RelayCommand]
    private async Task PreviewApplicationRegimeAsync(ApplicationRegime? item)
    {
        if(item is null) return;
        await UserDialogService.ShowAlertAsync("Преглед — начин на апликација",$"Начин на апликација:\n\n{item.Regime}","Затвори");
    }

    [RelayCommand]
    private async Task EditApplicationRegimeAsync(ApplicationRegime? item)
    {
        if(item is null) return;
        var value=await UserDialogService.ShowPromptAsync("Уреди режим на апликација","Изменете го режимот на апликација.","Зачувај","Откажи","",item.Regime);
        if(string.IsNullOrWhiteSpace(value)) return;
        value=value.Trim();
        await using var db=await _dbFactory.CreateDbContextAsync();
        if(await db.Set<ApplicationRegime>().AnyAsync(x=>x.Id!=item.Id&&x.IsActive&&x.Regime.ToLower()==value.ToLower()))
        {
            await UserDialogService.ShowAlertAsync("Постои запис",$"Начинот „{value}“ веќе постои.","ОК"); return;
        }
        var entity=await db.Set<ApplicationRegime>().FirstOrDefaultAsync(x=>x.Id==item.Id);
        if(entity is null) return;
        entity.Regime=value; entity.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync();
        await LoadApplicationRegimesAsync();
    }

    [RelayCommand]
    private async Task DeleteApplicationRegimeAsync(ApplicationRegime? item)
    {
        if(item is null) return;
        if(!await UserDialogService.ShowConfirmationAsync("Избриши режим на апликација",$"Дали сте сигурни дека сакате да го избришете режимот „{item.Regime}“?","Избриши","Откажи")) return;
        await using var db=await _dbFactory.CreateDbContextAsync();
        var entity=await db.Set<ApplicationRegime>().FirstOrDefaultAsync(x=>x.Id==item.Id);
        if(entity is null) return;
        entity.IsActive=false; entity.UpdatedAt=DateTime.UtcNow;
        await db.SaveChangesAsync();
        await LoadApplicationRegimesAsync();
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
            var actions = new List<SparkButtonItem>
            {
                new()
                {
                    IconGlyph="\uf06e",
                    Label="Детали",
                    Command=SelectCommand,
                    CommandParameter=m,
                    IsEnabled=CanView
                },
                new()
                {
                    IconGlyph="\uf044",
                    Label="Уреди",
                    Command=EditCommand,
                    CommandParameter=m,
                    IsEnabled=CanUpdate
                }
            };
            row["Actions"]=actions;
            actions?.Add(new SparkButtonItem
            {
                IconGlyph="\uf1f8",
                Label="Избриши",
                Command=DeleteMedicineCommand,
                CommandParameter=m,
                IsEnabled=CanDelete
            });
            rows.Add(row);
        }
        GridRows=rows;
    }
}