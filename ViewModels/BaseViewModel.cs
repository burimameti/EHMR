using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Desktop.Core.ViewModels;

public partial class BaseViewModel<T>
    : ObservableObject, IDisposable
{
    protected readonly INavigationService NavigationService;
    protected readonly IUserDialogService UserDialogService;
    protected readonly IMenuService MenuService;
    protected readonly IAuthStateService AuthService;

    protected CancellationTokenSource? SearchCts;
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    private readonly SynchronizationContext? _uiContext;

    protected CancellationTokenSource? _searchCts;
    protected List<T> _allItems = new();
    protected List<T> _filteredItems = new();

    private readonly int _pageSize = 10;
    private int _currentPage = 1;

    // =========================================================
    // OBSERVABLES
    // =========================================================

    [ObservableProperty] private ObservableCollection<T> items = new();
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool hasItems;

    public string SearchText = string.Empty;

    public string SearchTextValue
    {
        get => SearchText;
        set
        {
            if(SetProperty(ref SearchText, value))
                HandleSearchTextChanged(ApplyFilterAsync, value);
        }
    }

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if(SetProperty(ref _currentPage, value))
                UpdatePagination();
        }
    }

    // =========================================================
    // COMPUTED
    // =========================================================

    public bool HasPreviousPage => CurrentPage>1;
    public bool HasNextPage => CurrentPage*_pageSize<_filteredItems.Count;
    public int TotalItems => _filteredItems.Count;
    public int TotalPages => (int)Math.Ceiling((decimal)TotalItems/_pageSize);

    protected BaseViewModel(
    INavigationService navigationService,
    IUserDialogService userDialogService,
    IMenuService menuService,
    IAuthStateService authService)
    {
        NavigationService=navigationService;
        UserDialogService=userDialogService;
        MenuService=menuService;
        AuthService=authService;
        _uiContext=SynchronizationContext.Current;
    }

    // protected Guid? TenantId => AuthService?.CurrentUser?.TenantId;

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

    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    protected virtual void UpdatePagination()
    {
        OnPropertyChanged(nameof(HasNextPage));
        OnPropertyChanged(nameof(HasPreviousPage));
        OnPropertyChanged(nameof(TotalPages));
    }

    protected void HandleSearchTextChanged(Func<CancellationToken, Task> applyFilterAction, string value, int debounceMs = 400)
    {
        _searchCts?.Cancel();
        _searchCts=new CancellationTokenSource();
        var token = _searchCts.Token;

        _=Task.Run(async () =>
        {
            try
            {
                await Task.Delay(debounceMs, token);
                if(!token.IsCancellationRequested)
                    await applyFilterAction(token);
            }
            catch(TaskCanceledException) { }
        }, token);
    }

    protected void RefreshPage()
    {
        var pagedItems = _filteredItems
            .Skip((CurrentPage-1)*_pageSize)
            .Take(_pageSize)
            .ToList();

        Items.Clear();
        foreach(var item in pagedItems)
            Items.Add(item);

        HasItems=Items.Any();
        UpdatePagination();
    }

    protected virtual IEnumerable<T> FilterItems(string searchText, IEnumerable<T> items)
        => items;

    protected async Task ApplyFilterAsync(CancellationToken token = default)
    {
        try
        {
            _filteredItems=await Task.Run(() =>
            {
                token.ThrowIfCancellationRequested();
                return FilterItems(SearchText, _allItems).ToList();
            }, token);

            RunOnUiThread(() =>
            {
                CurrentPage=1;
                RefreshPage();
                OnPropertyChanged(nameof(TotalItems));
            });
        }
        catch(OperationCanceledException) { }
        catch(Exception ex)
        {
            OnError($"Failed to filter items: {ex.Message}");
        }
    }

    [RelayCommand]
    public Task NextPageAsync()
    {
        if(HasNextPage)
        {
            CurrentPage++;
            RunOnUiThread(RefreshPage);
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    public Task PreviousPageAsync()
    {
        if(HasPreviousPage)
        {
            CurrentPage--;
            RunOnUiThread(RefreshPage);
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    protected virtual async Task RefreshAsync()
    {
        if(IsBusy) return;
        IsRefreshing=true;
        IsBusy=true;
        ClearError();

        try
        {
            await OnRefreshAsync();
        }
        catch(Exception ex)
        {
            OnError(ex.Message);
        }
        finally
        {
            IsRefreshing=false;
            IsBusy=false;
        }
    }

    protected virtual Task OnRefreshAsync() => Task.CompletedTask;

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
        if(_uiContext!=null)
            _uiContext.Post(_ => action(), null);
        else
            action();
    }

    public void Dispose()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts=null;
    }
}