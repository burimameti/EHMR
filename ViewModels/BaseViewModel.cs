using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Domain.Entities.Rbac;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public abstract partial class BaseViewModel<T> : ObservableObject, IDisposable
{
    // ================= SERVICES =================
    protected readonly INavigationService NavigationService;
    protected readonly IUserDialogService UserDialogService;
    protected readonly IMenuService MenuService;
    protected readonly IAuthorizationService AuthService;
    protected readonly ISelectedItemService<T> SelectedItemService;

    protected SynchronizationContext? UiContext;

    protected virtual string DetailRoute =>
        throw new NotImplementedException($"Override {nameof(DetailRoute)} во derived VM.");

    // Override во derived VM ако сакаш поинаков текст по модул
    protected virtual string PermissionDeniedTitle => "Пристапот е одбиен";
    protected virtual string PermissionDeniedMessage => "Немате авторизација за оваа акција.";

    // ================= DATA =================
    protected List<T> AllItems = new();
    protected List<T> FilteredItems = new();

    [ObservableProperty] private ObservableCollection<T> items = new();

    // ================= STATE =================
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private bool hasItems;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;

    // ================= PAGINATION =================
    public int TotalItems => FilteredItems.Count;

    public int TotalPages =>
        PageSize<=0
            ? 0
            : (int)Math.Ceiling((double)TotalItems/PageSize);

    public string PageInfoText =>
        TotalPages<=0
            ? string.Empty
            : $"Страна {CurrentPage} од {TotalPages}";

    public bool HasNextPage => CurrentPage<TotalPages;
    public bool HasPreviousPage => CurrentPage>1;

    // ================= PERMISSIONS =================
    [ObservableProperty] private bool canCreate;
    [ObservableProperty] private bool canUpdate;
    [ObservableProperty] private bool canDelete;

    protected BaseViewModel(
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService,
        ISelectedItemService<T> selectedItemService)
    {
        NavigationService=navigationService;
        UserDialogService=userDialogService;
        MenuService=menuService;
        AuthService=authService;
        SelectedItemService=selectedItemService;

        UiContext=SynchronizationContext.Current;
    }

    protected abstract string ModuleName
    {
        get;
    }

    protected void EvaluatePermissions()
    {
        CanCreate=AuthService.CanPerform(ModuleName, ModuleAction.Create);
        CanUpdate=AuthService.CanPerform(ModuleName, ModuleAction.Edit);
        CanDelete=AuthService.CanPerform(ModuleName, ModuleAction.Delete);
    }

    // ================= SELECT / NEW / EDIT =================
    [RelayCommand]
    protected virtual async Task Select(T item)
    {
        SelectedItemService.SelectedItem=item;
        await NavigationService.GoToAsync(DetailRoute);
    }

    [RelayCommand]
    protected virtual async Task New()
    {
        if(!CanCreate)
        {
            await UserDialogService.ShowAlertAsync(PermissionDeniedTitle, PermissionDeniedMessage, "OK");
            return;
        }
        SelectedItemService.SelectedItem=default;
        await NavigationService.GoToAsync(DetailRoute);
    }

    [RelayCommand]
    protected virtual async Task Edit(T item)
    {
        if(!CanUpdate)
        {
            await UserDialogService.ShowAlertAsync(PermissionDeniedTitle, PermissionDeniedMessage, "OK");
            return;
        }
        SelectedItemService.SelectedItem=item;
        await NavigationService.GoToAsync(DetailRoute);
    }

    // ================= CORE PIPELINE =================
    protected void ApplyPipeline()
    {
        IEnumerable<T> query = AllItems;

        query=ApplySearch(query, SearchText);
        query=ApplyFilters(query);
        query=ApplySort(query);

        FilteredItems=query.ToList();

        CurrentPage=1;

        RefreshPage();

        OnPipelineApplied();
    }

    protected virtual void OnPipelineApplied()
    {
    }

    protected void RefreshPage()
    {
        var page = FilteredItems
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
            .ToList();

        Items=new ObservableCollection<T>(page);
        HasItems=Items.Any();

        RefreshPaginationNotifications();

        OnPageProjected(Items);
    }

    protected virtual void OnPageProjected(ObservableCollection<T> page)
    {
    }

    // ================= HOOKS =================
    protected abstract IEnumerable<T> ApplyFilters(IEnumerable<T> query);

    protected virtual IEnumerable<T> ApplySearch(IEnumerable<T> query, string search)
        => query;

    protected virtual IEnumerable<T> ApplySort(IEnumerable<T> query)
        => query;

    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if(SetProperty(ref _searchText, value))
            {
                CurrentPage=1;
                ApplyPipeline();
            }
        }
    }

    private int _currentPage = 1;

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if(SetProperty(ref _currentPage, value))
            {
                RefreshPaginationNotifications();
                RefreshPage();
            }
        }
    }

    private int _pageSize = 10;

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if(SetProperty(ref _pageSize, value))
            {
                CurrentPage=1;
                RefreshPaginationNotifications();
                RefreshPage();
            }
        }
    }

    // ================= PAGINATION COMMANDS =================
    [RelayCommand]
    private void PageChanged(int page)
    {
        if(page<1||page>TotalPages||page==CurrentPage) return;
        CurrentPage=page;
    }

    [RelayCommand]
    public void NextPage()
    {
        if(HasNextPage)
            CurrentPage++;
    }

    [RelayCommand]
    public void PreviousPage()
    {
        if(HasPreviousPage)
            CurrentPage--;
    }

    [RelayCommand]
    public void Reset()
    {
        CurrentPage=1;
        ApplyPipeline();
    }

    // ================= SAFE EXECUTION =================
    protected async Task ExecuteSafeAsync(Func<Task> action, string errorMessage)
    {
        if(IsBusy) return;

        try
        {
            IsBusy=true;
            ClearError();

            await action();
        }
        catch(Exception ex)
        {
            OnError($"{errorMessage}: {ex.Message}");
        }
        finally
        {
            IsBusy=false;
        }
    }

    // ================= UI HELPERS =================
    protected void OnError(string message)
    {
        ErrorMessage=message;
        HasError=true;
    }

    protected void ClearError()
    {
        ErrorMessage=string.Empty;
        HasError=false;
    }

    protected void RunOnUiThread(Action action)
    {
        if(UiContext!=null)
            UiContext.Post(_ => action(), null);
        else
            action();
    }

    public void Dispose()
    {
    }

    // ================= SPARK BUTTONS =================
    public ObservableCollection<SparkButtonItem> Buttons { get; } = new();

    protected virtual void BuildSparkButtons()
    {
        Buttons.Clear();
        Buttons.Add(new SparkButtonItem
        {
            Label="✕ Исчисти",
            IsPrimary=true,
            Command=ClearFiltersCommand
        });
    }

    // ================= SPARK PICKERS =================
    public ObservableCollection<SparkPickerItem> Pickers { get; } = new();

    protected static SparkPickerItem MakePicker(string placeholder, IEnumerable<string> items,
        string initialSelection, Action<string> onSelected)
    {
        var picker = new SparkPickerItem { Placeholder=placeholder };
        foreach(var item in items) picker.Items.Add(item);
        picker.SelectedItem=initialSelection;

        picker.PropertyChanged+=(_, e) =>
        {
            if(e.PropertyName==nameof(SparkPickerItem.SelectedItem)&&picker.SelectedItem is string s)
                onSelected(s);
        };
        return picker;
    }

    protected virtual void SyncSparkPickersFromFilters()
    {
    }

    protected void RefreshPaginationNotifications()
    {
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageInfoText));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(HasPreviousPage));
    }

    // ================= CLEAR FILTERS =================
    [RelayCommand]
    protected void ClearFilters()
    {
        ResetFilters();
        ApplyPipeline();
        SyncSparkPickersFromFilters();
    }

    protected abstract void ResetFilters();

    protected virtual void AddDefaultActions(T item, SparkGridRow row, string detailLabel = "Детали", string editLabel = "✎")
    {
        var actions = new List<SparkButtonItem>
    {
        new SparkButtonItem
        {
            IsPrimary=true,
            IconGlyph="👁",
            Label=detailLabel,
            Command=SelectCommand,
            CommandParameter=item
        }
    };

        if(CanUpdate)
            actions.Add(new SparkButtonItem { Label=editLabel, Command=EditCommand, CommandParameter=item });

        row["Actions"]=actions;
    }

    // BaseViewModel<T>
    // ================= GENERIC TAB FILTERS =================
    public ObservableCollection<SparkTabItem> Tabs { get; } = new();

    protected readonly Dictionary<string, SparkTabItem> TabsByKey = new();

    /// <summary>
    /// Гради tabs од произволна листа опции. Derived VM одлучува label/key/selection/onSelect,
    /// база само раководи со UI state (selection toggle, dictionary lookup).
    /// </summary>
    protected void BuildTabFilters<TOption>(
        IEnumerable<TOption> options,
        Func<TOption, string> keySelector,
        Func<TOption, string> labelSelector,
        Func<TOption, bool> isSelectedSelector,
        Action<TOption> onSelect)
    {
        Tabs.Clear();
        TabsByKey.Clear();

        foreach(var option in options)
        {
            var tab = new SparkTabItem
            {
                Title=labelSelector(option),
                IsSelected=isSelectedSelector(option)
            };

            tab.Command=new RelayCommand(() => SelectTab(tab, () => onSelect(option)));

            Tabs.Add(tab);
            TabsByKey[keySelector(option)]=tab;
        }
    }

    protected void SelectTab(SparkTabItem tab, Action action)
    {
        foreach(var t in Tabs) t.IsSelected=false;
        tab.IsSelected=true;
        action();
    }

    protected void RefreshTabCount(string key, int count)
    {
        if(TabsByKey.TryGetValue(key, out var tab))
            tab.Value=count.ToString("N0");
    }

    protected void SyncTabsFromKey(string selectedKey)
    {
        foreach(var (key, tab) in TabsByKey)
            tab.IsSelected=key==selectedKey;
    }   
}