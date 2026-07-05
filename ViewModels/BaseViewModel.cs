using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public abstract partial class BaseViewModel<T> : ObservableObject, IDisposable
{
    // ================= SERVICES =================
    protected readonly INavigationService NavigationService;

    protected readonly IUserDialogService UserDialogService;
    protected readonly IMenuService MenuService;
    protected readonly IAuthorizationService AuthService;

    protected SynchronizationContext? UiContext;

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

    // ================= PAGING =================

    public int TotalItems => FilteredItems.Count;

    public bool HasNextPage => CurrentPage<TotalPages;
    public bool HasPreviousPage => CurrentPage>1;

    protected BaseViewModel(
        INavigationService navigationService,
        IUserDialogService userDialogService,
        IMenuService menuService,
        IAuthorizationService authService)
    {
        NavigationService=navigationService;
        UserDialogService=userDialogService;
        MenuService=menuService;
        AuthService=authService;

        UiContext=SynchronizationContext.Current;
    }

    // ================= CORE PIPELINE =================
    protected void ApplyPipeline()
    {
        IEnumerable<T> query = AllItems;

        query=ApplySearch(query, SearchText);
        query=ApplyFilters(query);
        query=ApplySort(query);

        FilteredItems=query.ToList();

        // Bypass the property setter to avoid duplicate calls to RefreshPage
        _currentPage=1;

        RefreshPage();
    }

    protected void RefreshPage()
    {
        var page = FilteredItems
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
            .ToList();

        Items=new ObservableCollection<T>(page);

        HasItems=Items.Any();

        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(HasPreviousPage));
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
                RefreshPage();
            }
        }
    }

    private int _pageSize = 10;

    public int TotalPages =>
           PageSize<=0 ? 0 : (int)Math.Ceiling((double)TotalItems/PageSize);

    public int PageSize
    {
        get => _pageSize;
        set
        {
            if(SetProperty(ref _pageSize, value))
            {
                CurrentPage=1;   // important reset
                RefreshPage();
            }
        }
    }

    // ================= PAGINATION COMMANDS =================
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
}