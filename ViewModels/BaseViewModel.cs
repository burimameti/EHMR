using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.Desktop.Core.ViewModels;

public partial class BaseViewModel<T>
    : ObservableObject, IDisposable
{
    protected readonly INavigationService NavigationService;
    protected readonly IUserDialogService UserDialogService;
    protected readonly IMenuService MenuService;
    protected readonly IAuthorizationService AuthorizationService;

    private readonly SynchronizationContext? _uiContext;

    protected CancellationTokenSource? _searchCts;
    protected List<T> _allItems = new();
    protected List<T> _filteredItems = new();

    private int _currentPage = 1;
    private int _pageSize = 10;

    // =========================================================
    // OBSERVABLES
    // =========================================================

    [ObservableProperty] private ObservableCollection<T> items = new();
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isRefreshing;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool hasItems;

    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if(SetProperty(ref _searchText, value))
            {
                HandleSearchTextChanged(ApplyFilterAsync, value);
            }
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

    /// <summary>
    /// Number of rows shown per page. Changing this resets to page 1 and
    /// re-renders the current page immediately.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if(value<1) value=1;
            if(SetProperty(ref _pageSize, value))
            {
                CurrentPage=1;
                RefreshPage();
            }
        }
    }

    // =========================================================
    // COMPUTED
    // =========================================================

    public bool HasPreviousPage => CurrentPage>1;
    public bool HasNextPage => CurrentPage*PageSize<_filteredItems.Count;
    public int TotalItems => _filteredItems.Count;
    public int TotalPages => TotalItems==0 ? 1 : (int)Math.Ceiling((decimal)TotalItems/PageSize);

    /// <summary>
    /// Human readable "Showing 1-10 of 48" style summary for grid footers.
    /// </summary>
    public string PaginationSummary
    {
        get
        {
            if(TotalItems==0) return "Нема резултати";

            int start = (CurrentPage-1)*PageSize+1;
            int end = Math.Min(CurrentPage*PageSize, TotalItems);
            return $"Прикажани {start}–{end} од {TotalItems}";
        }
    }

    protected BaseViewModel(
      INavigationService navigationService,
      IUserDialogService userDialogService,
      IMenuService menuService,
      IAuthorizationService authorizationService)
    {
        NavigationService=navigationService;
        UserDialogService=userDialogService;
        MenuService=menuService;
        AuthorizationService=authorizationService;

        _uiContext=SynchronizationContext.Current;
    }

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
        OnPropertyChanged(nameof(PaginationSummary));
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
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
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
                OnPropertyChanged(nameof(PaginationSummary));
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