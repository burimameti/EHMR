using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Maui.Graphics;

namespace EHMR.ViewModels;

public partial class MedicineDetailFormViewModel : ObservableObject
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly ISelectedItemService<Medicine> _selectionService;
    private readonly INavigationService _navigationService;
    private readonly IUserDialogService _dialogService;
    private readonly IAuthorizationService _authorization;

    private Medicine? _originalMedicine;
    private bool _isNewMode;

    #region Observable Properties

    [ObservableProperty]
    private Medicine medicine = new();

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private bool isReadOnly = true;

    [ObservableProperty]
    private string pageTitle = string.Empty;

    [ObservableProperty]
    private string pageSubtitle = string.Empty;

    // ── ТАБОВИ ──────────────────────────────────────────────────────────
    //
    // Страницата има три таба и врзуваше IsTab1Active/IsTab2Active/IsTab3Active
    // и SelectTabCommand — ниту едно од нив не постоеше. Кога врзувањето падне
    // IsVisible се враќа на true, па сите три секции се прикажуваа една под друга,
    // а кликот на таб не правеше ништо.

    [ObservableProperty]
    private int activeTab = 1;

    #endregion

    #region Computed Properties

    public bool IsTab1Active => ActiveTab==1;
    public bool IsTab2Active => ActiveTab==2;
    public bool IsTab3Active => ActiveTab==3;

    partial void OnActiveTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsTab1Active));
        OnPropertyChanged(nameof(IsTab2Active));
        OnPropertyChanged(nameof(IsTab3Active));
    }

    public bool IsEditMode => !IsReadOnly;
    public bool CanCreate => _authorization.CanPerform(Modules.Inventory, ModuleAction.Create);
    public bool CanUpdate => _authorization.CanPerform(Modules.Inventory, ModuleAction.Edit);
    public bool CanDelete => _authorization.CanPerform(Modules.Inventory, ModuleAction.Delete);
    public bool CanEditForm => _isNewMode ? CanCreate : CanUpdate;

    public string HeaderTitle =>
        _isNewMode
            ? "➕ Нов лек"
            : $"💊 {Medicine.Name}";

    public string HeaderSubtitle =>
        _isNewMode
            ? "Креирање на нов лек"
            : $"Генеричко име: {Medicine.GenericName}";

    public Color InputBackground =>
        IsReadOnly
            ? Color.FromArgb("#F8FAFC")
            : Colors.White;

    public Color InputBorder =>
        IsReadOnly
            ? Color.FromArgb("#E5E7EB")
            : Color.FromArgb("#4F8CFF");

    #endregion

    public MedicineDetailFormViewModel(
        IDbContextFactory<DesktopTherapyDbContext> dbFactory,
        ISelectedItemService<Medicine> selectionService,
        INavigationService navigationService,
        IUserDialogService dialogService,
        IAuthorizationService authorization)
    {
        _dbFactory=dbFactory;
        _selectionService=selectionService;
        _navigationService=navigationService;
        _dialogService=dialogService;
        _authorization=authorization;

        Initialize();
    }

    private void Initialize()
    {
        var selected = _selectionService.SelectedItem;

        if(selected==null)
        {
            Medicine=new Medicine();

            // Тука `selected` е null по дефиниција — Clone(selected) го читаше
            // source.Id и фрлаше NullReferenceException штом ќе се притисне „Нов лек".
            // Основата за споредба е самиот празен лек.
            _originalMedicine=Medicine.Clone();

            _isNewMode=true;
            IsReadOnly=false;

            PageTitle="Нов лек";
            PageSubtitle="Внес на нов лек";
        }
        else
        {
            Medicine=selected.Clone();

            _originalMedicine=selected.Clone();

            _isNewMode=false;
            IsReadOnly=true;

            PageTitle="Детали за лек";
            PageSubtitle=selected.Name;
        }

        RefreshState();
    }

    partial void OnIsReadOnlyChanged(bool value)
    {
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(InputBackground));
        OnPropertyChanged(nameof(InputBorder));
    }

    partial void OnMedicineChanged(Medicine value)
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
    }

    private void RefreshState()
    {
        OnPropertyChanged(nameof(HeaderTitle));
        OnPropertyChanged(nameof(HeaderSubtitle));
        OnPropertyChanged(nameof(InputBackground));
        OnPropertyChanged(nameof(InputBorder));
        OnPropertyChanged(nameof(IsEditMode));
    }

    /// <summary>Префрлање меѓу трите таба. Параметарот доаѓа од XAML како "1" / "2" / "3".</summary>
    [RelayCommand]
    private void SelectTab(string? tab)
    {
        if(int.TryParse(tab, out var index)&&index>=1&&index<=3)
            ActiveTab=index;
    }

    [RelayCommand]
    private void ToggleEdit()
    {
        IsReadOnly=false;

        PageTitle="Измена на лек";
        PageSubtitle=Medicine.Name;

        RefreshState();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if(_isNewMode)
        {
            await _navigationService.GoToAsync("..");
            return;
        }

        if(_originalMedicine!=null)
            Medicine=_originalMedicine.Clone();

        IsReadOnly=true;

        PageTitle="Детали за лек";
        PageSubtitle=Medicine.Name;

        RefreshState();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if(IsBusy)
            return;

        if(string.IsNullOrWhiteSpace(Medicine.Name))
        {
            await _dialogService.ShowAlertAsync(
                "Валидација",
                "Името на лекот е задолжително.",
                "ОК");

            return;
        }

        try
        {
            IsBusy=true;

            await using var db = await _dbFactory.CreateDbContextAsync();

            if(_isNewMode)
            {
                await db.Set<Medicine>().AddAsync(Medicine);

                await db.SaveChangesAsync();

                await _dialogService.ShowAlertAsync(
                    "Успешно",
                    "Лекот е успешно додаден.",
                    "ОК");
            }
            else
            {
                db.Set<Medicine>().Update(Medicine);

                await db.SaveChangesAsync();

                await _dialogService.ShowAlertAsync(
                    "Успешно",
                    "Промените се успешно зачувани.",
                    "ОК");
            }

            _selectionService.SelectedItem=Medicine;

            _originalMedicine=Medicine.Clone();

            IsReadOnly=true;

            RefreshState();

            await _navigationService.GoToAsync("..");
        }
        catch(Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                "Грешка",
                ex.Message,
                "ОК");
        }
        finally
        {
            IsBusy=false;
        }
    }
}