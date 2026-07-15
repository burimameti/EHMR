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

    protected override string ModuleName => "users";
    protected override string DetailRoute => AppRoutes.Users.Detail;
    protected override string PermissionDeniedMessage => "Немате авторизација за додавање нов корисник.";

    public ICommand SearchCommand
    {
        get;
    }

    public UsersViewModel(
        IUserService userService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<UserAdminDto> selectedItemService,
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService, selectedItemService)
    {
        _userService=userService;

        PageSize=10;
        SearchCommand=new Command<string>(query => SearchText=query);

        EvaluatePermissions();
        BuildSparkGridColumns();
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

            AddDefaultActions(u, row);

            rows.Add(row);
        }

        GridRows=rows;
    }
}