using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Views.Therapies; // Промени го со точниот namespace каде ќе биде формата
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
            {
                _=ApplyFilterAsync();
            }
        }
    }

    // Листа за филтрирање по медицински гранки (можеш да ја дополниш од база или статички)
    public List<string> CategoryFilters { get; } = ["Сите", "Кардиологија", "Онкологија", "Нефрологија", "Пулмологија"];

    public ProtocolRegistryViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<TherapyProtocol> selectedItemService,
        IAuthStateService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
    }

    // LIFECYCLE
    public override async Task OnAppearingAsync()
    {
        await LoadAsync();
    }

    protected override async Task OnRefreshAsync()
    {
        await LoadAsync();
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

            _allItems=protocols; // Полнење на заштитената база од твојот BaseViewModel

            await ApplyFilterAsync();
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
    protected override IEnumerable<TherapyProtocol> FilterItems(string searchText, IEnumerable<TherapyProtocol> items)
    {
        var query = items;

        // 1. Пребарување по текст (Име или Опис)
        if(!string.IsNullOrWhiteSpace(searchText))
        {
            query=query.Where(x =>
                (x.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false));
        }

        // 2. Филтрирање по Категорија
        if(SelectedCategory!="Сите")
        {
            query=query.Where(x => x.DiseaseCategory==SelectedCategory);
        }

        return query;
    }

    // ACTIONS

    [RelayCommand]
    private async Task AddAsync()
    {
        _selectedItemService.SelectedItem=null;
        await NavigationService.GoToAsync(AppRoutes.Protocols.Details);
    }

    [RelayCommand]
    private async Task SelectAsync(TherapyProtocol protocol)
    {
        if(protocol==null) return;

        _selectedItemService.SelectedItem=protocol; // Праќаме постоечки -> ПРЕГЛЕД/ИЗМЕНА
        await NavigationService.GoToAsync(AppRoutes.Protocols.Details);
    }

    [RelayCommand]
    private async Task EditAsync(TherapyProtocol protocol)
    {
        if(protocol==null) return;

        _selectedItemService.SelectedItem=protocol;
        await NavigationService.GoToAsync(AppRoutes.Protocols.Details);
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

            _allItems.Remove(protocol);
            await ApplyFilterAsync();

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

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText=string.Empty;
        SelectedCategory="Сите";
        await ApplyFilterAsync();
    }
}