using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using EHMR.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace EHMR.ViewModels;

public partial class UsersViewModel : BaseViewModel<UserAdminDto>
{
    private readonly IUserService _userService;
    private readonly ISelectedItemService<UserAdminDto> _userSelectionService;

    protected override string ModuleName => "users";

    public ICommand SearchCommand
    {
        get;
    }

    public UsersViewModel(
        IUserService userService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<UserAdminDto> userSelectionService,
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _userService=userService;
        _userSelectionService=userSelectionService;

        PageSize=10;
        SearchCommand=new Command<string>(query => SearchText=query);

        EvaluatePermissions();
        BuildSparkGridColumns(); // static columns — no need to rebuild per load
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

            var data = await _userService.GetUsersAsync();
            AllItems=data.ToList();
            ApplyPipeline();
        }
        catch(Exception ex)
        {
            OnError($"Failed to load users: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // ================= SEARCH =================
    protected override IEnumerable<UserAdminDto> ApplySearch(
        IEnumerable<UserAdminDto> items,
        string searchText)
    {
        if(string.IsNullOrWhiteSpace(searchText))
            return items;

        return items.Where(x =>
            (x.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
            (x.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
            (x.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
            ($"{x.FirstName} {x.LastName}".Contains(searchText, StringComparison.OrdinalIgnoreCase)));
    }

    // ================= FILTERS =================
    protected override IEnumerable<UserAdminDto> ApplyFilters(IEnumerable<UserAdminDto> query)
    {
        // Add future filters here (role, status, etc.)
        return query;
    }

    // ================= SORT =================
    protected override IEnumerable<UserAdminDto> ApplySort(IEnumerable<UserAdminDto> query)
    {
        return query.OrderByDescending(x => x.Username);
    }

    protected override void ResetFilters()
    {
        SearchText=string.Empty;
        // Add resets here alongside any future filter added to ApplyFilters above.
    }

    // ================= SPARK GRID =================
    [ObservableProperty] private ObservableCollection<SparkGridColumn> gridColumns = new();
    [ObservableProperty] private ObservableCollection<SparkGridRow> gridRows = new();

    private void BuildSparkGridColumns()
    {
        GridColumns=new ObservableCollection<SparkGridColumn>
        {
            new() { Header = "КОРИСНИЧКО ИМЕ", Key = "Username", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "ИМЕ И ПРЕЗИМЕ", Key = "FullName", Width = new GridLength(2, GridUnitType.Star) },
            new() { Header = "РОЛА", Key = "Role", Width = new GridLength(1.5, GridUnitType.Star) },
            new() { Header = "СТАТУС", Key = "Status", CellType = SparkGridCellType.Badge, Width = new GridLength(1.2, GridUnitType.Star) },
            new() { Header = "АКЦИИ", Key = "Actions", CellType = SparkGridCellType.Actions, Width = GridLength.Auto }
        };
    }

    protected override void OnPageProjected(ObservableCollection<UserAdminDto> page)
    {
        var rows = new ObservableCollection<SparkGridRow>();

        foreach(var u in page)
        {
            var row = new SparkGridRow { Tag=u };
            row["Username"]=u.Username;
            row["FullName"]=$"{u.FirstName} {u.LastName}";
            row["Role"]=u.Role;
            row["Status"]=new SparkBadgeValue(
                u.IsActive ? "АКТИВЕН" : "НЕАКТИВЕН",
                u.IsActive ? SparkBadgeTone.Success : SparkBadgeTone.Danger);
            rows.Add(row);
        }

        GridRows=rows;
    }

    // ================= ACTIONS =================
    // Renamed Create → Add and SelectUser → Select to match the naming convention
    // every other list screen uses (AddCommand / SelectCommand / EditCommand),
    // so the SparkDataGridView bindings line up the same way they do on PatientListPage.
    [RelayCommand]
    private async Task Add()
    {
        _userSelectionService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }

    [RelayCommand]
    private async Task Select(UserAdminDto user)
    {
        if(user==null) return;
        _userSelectionService.SelectedItem=user;
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }

    [RelayCommand]
    private async Task Edit(UserAdminDto user)
    {
        if(user==null) return;
        _userSelectionService.SelectedItem=user;
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }
}