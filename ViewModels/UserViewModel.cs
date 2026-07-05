using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Services;

namespace EHMR.ViewModels;

public partial class UsersViewModel : BaseViewModel<UserAdminDto>
{
    private readonly IUserService _userService;
    private readonly ISelectedItemService<UserAdminDto> _userSelectionService;

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
    }

    // ================= LOAD =================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;

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

    // ================= SORT (OPTIONAL) =================
    protected override IEnumerable<UserAdminDto> ApplySort(IEnumerable<UserAdminDto> query)
    {
        return query.OrderByDescending(x => x.Username);
    }

    // ================= ACTIONS =================
    [RelayCommand]
    public async Task CreateAsync()
    {
        _userSelectionService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }

    [RelayCommand]
    public async Task SelectUser(UserAdminDto user)
    {
        if(user==null) return;

        _userSelectionService.SelectedItem=user;
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }
}