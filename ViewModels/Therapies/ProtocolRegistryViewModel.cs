using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;

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
                ApplyPipeline();
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
        IAuthorizationService authService)
        : base(navigationService, userDialogService, menuService, authService)
    {
        _dbFactory=dbFactory;
        _selectedItemService=selectedItemService;
    }

    // LIFECYCLE

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

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText=string.Empty;
        SelectedCategory="Сите";
        ApplyPipeline();
    }
}