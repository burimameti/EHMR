using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Desktop.Core.ViewModels; // Патеката каде што ти е BaseViewModel
using EHMR.Domain.Interfaces;
using EHMR.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

// Го наследуваме BaseViewModel за да ги добиеме NavigationService и UserDialogService автоматски
public partial class UsersViewModel : BaseViewModel<UserAdminDto>
{
    private readonly IUserAdminService _userService;
    private readonly ISelectedItemService<UserAdminDto> _userSelectionService;

    // Внатрешна листа која ја користи BaseViewModel за филтрирање во меморија
    private List<UserAdminDto> _allUsers = new();

    // Пребарување со реактивен сетер - штом се смени текстот, веднаш филтрира
    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if(SetProperty(ref _searchText, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    // Листа која е врзана директно за CollectionView (Data Grid) на екранот
    [ObservableProperty]
    private ObservableCollection<UserAdminDto> users = new();

    // =========================================================
    // CTOR
    // =========================================================
    public UsersViewModel(
        IUserAdminService userService,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<UserAdminDto> userSelectionService,
        IAuthStateService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _userService=userService;
        _userSelectionService=userSelectionService;
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================
    public override async Task OnAppearingAsync()
    {
        await LoadAsync();
    }

    protected override async Task OnRefreshAsync()
    {
        await LoadAsync();
    }

    // =========================================================
    // LOAD DATA
    // =========================================================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            // ClearError(); // Ако ја има оваа метода во твојот BaseViewModel

            var data = await _userService.GetUsersAsync();
            _allUsers=data.ToList();

            // Ги поставуваме филтрираните ставки во реалната колекција на UI
            await ApplyFilterAsync();
        }
        catch(Exception ex)
        {
            // OnError($"Неуспешно вчитување: {ex.Message}");
            await UserDialogService.ShowAlertAsync("Грешка", $"Неуспешно вчитување на корисници: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // FILTERING LOGIC (Имплементација на FilterItems од твојот Base)
    // =========================================================
    protected override IEnumerable<UserAdminDto> FilterItems(string searchText, IEnumerable<UserAdminDto> items)
    {
        var query = items;

        if(!string.IsNullOrWhiteSpace(searchText))
        {
            query=query.Where(x =>
                (x.Username?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                ($"{x.FirstName} {x.LastName}").Contains(searchText, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }

    // Помошен метод за синхронизација на BaseViewModel со ObservableCollection на овој екран
    private async Task ApplyFilterAsync()
    {
        // Претпоставуваме дека BaseViewModel ја обработува филтрираната листа во метод кој враќа пресметани ставки,
        // или рачно ги мапираме филтрираните податоци
        var filtered = FilterItems(SearchText, _allUsers);
        Users=new ObservableCollection<UserAdminDto>(filtered);
        await Task.CompletedTask;
    }

    // =========================================================
    // ACTIONS & NAVIGATION (Одење во посебен екран како кај пациентите)
    // =========================================================

    [RelayCommand]
    public async Task CreateAsync()
    {
        // Селектираме null за формата да сфати дека правиме САСМА нов запис
        _userSelectionService.SelectedItem=null;

        // Навигација до формата за креирање/уредување
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }

    [RelayCommand]
    public async Task SelectUser(UserAdminDto user)
    {
        if(user==null) return;

        // Го ставаме во глобалниот селектор за формата да може да го повлече и клонира кај неа
        _userSelectionService.SelectedItem=user;

        // Навигираме до страницата за уредување
        await NavigationService.GoToAsync(AppRoutes.Users.Detail);
    }

    [RelayCommand]
    private async Task Reset(UserAdminDto user)
    {
        if(user==null) return;

        bool confirm = await UserDialogService.ShowConfirmationAsync(
            "Потврда за бришење",
            $"Дали сте сигурни дека сакате трајно да го избришете корисникот '{user.Username}'?");

        if(!confirm) return;

        try
        {
            IsBusy=true;
            // Овде ја повикуваш твојата логика за бришење од сервис
            // await _userService.DeleteAsync(user.Id);

            _allUsers.Remove(user);
            await ApplyFilterAsync();

            await UserDialogService.ShowAlertAsync("Успешно", "Корисникот е избришан од системот.", "OK");
        }
        catch(Exception ex)
        {
            await UserDialogService.ShowAlertAsync("Грешка", $"Неуспешно бришење: {ex.Message}", "OK");
        }
        finally
        {
            IsBusy=false;
        }
    }
}