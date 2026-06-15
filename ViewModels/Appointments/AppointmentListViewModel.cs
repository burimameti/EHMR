using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Constants;
using EHMR.Desktop.Core.ViewModels;
using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using EHMR.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.ViewModels;

public partial class AppointmentListViewModel : BaseViewModel<Appointment>
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Appointment> _selectedItemService;
    private readonly IAuthorizationPolicy _authService; // Наместо застарени проверки, оди преку рефакторираниот сервис

    [ObservableProperty]
    private Appointment? _selectedAppointment;

    // =========================================================
    // FILTER VALUES (Класичен начин со рачен SetProperty)
    // =========================================================
    private string _selectedStatus = "Сите";

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if(SetProperty(ref _selectedStatus, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    private DateTime _filterDate = DateTime.Today;

    public DateTime FilterDate
    {
        get => _filterDate;
        set
        {
            if(SetProperty(ref _filterDate, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    private bool _filterByDate = false;

    public bool FilterByDate
    {
        get => _filterByDate;
        set
        {
            if(SetProperty(ref _filterByDate, value))
            {
                _=ApplyFilterAsync();
            }
        }
    }

    public List<string> StatusFilters { get; } = ["Сите", "Scheduled", "Completed", "Canceled", "NoShow"];

    // =========================================================
    // НАПРЕДНА ПРОВЕРКА НА НИВО НА МОДУЛ
    // =========================================================
    /// <summary>
    /// Проверува дали корисникот има пристап до целиот менаџмент на термини и календар.
    /// </summary>
    public bool CanManageAppointments => _authService.CanAccess(Modules.Appointments);

    // =========================================================
    // CTOR
    // =========================================================
    public AppointmentListViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        ISelectedItemService<Appointment> selectedItemService,
        IAuthStateService authStateService,
        IAuthorizationPolicy authService)
        : base(navigationService, userDialogService, menuService, authStateService)
    {
        _dbFactory=dbFactory;
        _authService=authService;
        _selectedItemService=selectedItemService;
    }

    protected override async Task OnRefreshAsync() => await LoadAsync();

    // =========================================================
    // LOAD
    // =========================================================
    [RelayCommand]
    public async Task LoadAsync()
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            await using var db = await _dbFactory.CreateDbContextAsync();

            // Вчитување со Include за да имаме податоци за Пациентот и Докторот во табелата
            var appointments = await db.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsNoTracking()
                .OrderBy(x => x.ScheduledStart)
                .ToListAsync();

            _allItems=appointments;

            await ApplyFilterAsync();
        }
        catch(Exception ex)
        {
            OnError($"Грешка при вчитување термини: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // =========================================================
    // FILTERING
    // =========================================================
    protected override IEnumerable<Appointment> FilterItems(string searchText, IEnumerable<Appointment> items)
    {
        var query = items;

        // 1. Пребарување по име на пациент или доктор
        if(!string.IsNullOrWhiteSpace(searchText))
        {
            query=query.Where(x =>
                (x.Patient?.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Patient?.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Doctor?.FirstName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.Doctor?.LastName?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false)||
                (x.ReasonForVisit?.Contains(searchText, StringComparison.OrdinalIgnoreCase)??false));
        }

        // 2. Филтер по статус
        if(SelectedStatus!="Сите")
        {
            query=query.Where(x => x.Status.ToString()==SelectedStatus);
        }

        // 3. Филтер по конкретен датум
        if(FilterByDate)
        {
            query=query.Where(x => x.ScheduledStart.Date==FilterDate.Date);
        }

        return query;
    }

    // =========================================================
    // ACTIONS (Безбедна генеричка навигација врзана со менаџмент на модули)
    // =========================================================
    [RelayCommand]
    private async Task AddAsync()
    {
        if(!CanManageAppointments)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за додавање на нови термини.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=null; // Нов термин
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task SelectAsync(Appointment appointment)
    {
        if(appointment==null) return;
        _selectedItemService.SelectedItem=appointment;
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task EditAsync(Appointment appointment)
    {
        if(appointment==null) return;

        // Рефакторирано: Наместо Perm.AppointmentsUpdate, проверуваме пристап до целиот Appointments модул
        if(!CanManageAppointments)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за уредување на термини.", "OK");
            return;
        }

        _selectedItemService.SelectedItem=appointment;
        await NavigationService.GoToAsync(AppRoutes.Appointments.Detail);
    }

    [RelayCommand]
    private async Task CancelAsync(Appointment appointment)
    {
        if(appointment==null) return;

        if(!CanManageAppointments)
        {
            await UserDialogService.ShowAlertAsync("Пристапот е одбиен", "Немате авторизација за откажување на термини.", "OK");
            return;
        }

        bool confirm = await UserDialogService.ShowConfirmationAsync("Потврда", "Дали сакате да го откажете овој термин?");
        if(!confirm) return;

        try
        {
            IsBusy=true;
            await using var db = await _dbFactory.CreateDbContextAsync();
            appointment.Status = AppointmentStatus.Cancelled;
            db.Appointments.Update(appointment);
            await db.SaveChangesAsync();

            _allItems.Remove(appointment);
            await ApplyFilterAsync();

            await UserDialogService.ShowAlertAsync("Успешно", "Терминот е откажан.", "OK");
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
        SelectedStatus="Сите";
        FilterByDate=false;
        await ApplyFilterAsync();
    }
}