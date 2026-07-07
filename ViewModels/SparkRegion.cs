using CommunityToolkit.Mvvm.ComponentModel;
using EHMR.Resources.Controls;
using System.Collections.ObjectModel;

namespace EHMR.ViewModels;

public partial class SparkRegion<T> : ObservableObject
{
    // ================= DATA =================
    private List<T> _source = new();
    private List<T> _filtered = new();

    // ================= OUTPUT =================
    public ObservableCollection<T> PagedItems { get; private set; } = new();
    public ObservableCollection<SparkGridRow> Rows { get; private set; } = new();

    public ObservableCollection<SparkGridColumn> Columns { get; } = new();

    public ObservableCollection<SparkGridColumn> GridColumns => Columns;
    public ObservableCollection<SparkGridRow> GridRows => Rows;

    // ================= STATE =================
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int totalPages = 1;

    public int PageSize { get; set; } = 10;

    // ================= PIPELINE CONFIG =================
    public string SearchText { get; private set; } = string.Empty;

    public Func<T, bool>? FilterPredicate
    {
        get; set;
    }
    public Func<T, string, bool>? SearchPredicate
    {
        get; set;
    }
    public Func<T, SparkGridRow>? RowMapper
    {
        get; set;
    }

    // ================= SOURCE =================
    public void SetSource(IEnumerable<T> data)
    {
        _source=data?.ToList()??new();
        CurrentPage=1;
        Invalidate();
    }

    // ================= EXTERNAL STATE =================
    public void SetSearch(string? search)
    {
        SearchText=search??string.Empty;
        CurrentPage=1;
        Invalidate();
    }

    public void SetPage(int page)
    {
        CurrentPage=page;
        Apply();
    }

    public void NextPage()
    {
        if(CurrentPage<TotalPages)
        {
            CurrentPage++;
            Apply();
        }
    }

    public void PreviousPage()
    {
        if(CurrentPage>1)
        {
            CurrentPage--;
            Apply();
        }
    }

    // ================= PIPELINE =================
    public void Invalidate()
    {
        Apply();
    }

    public void Apply()
    {
        IEnumerable<T> query = _source;

        // SEARCH
        if(!string.IsNullOrWhiteSpace(SearchText)&&SearchPredicate!=null)
            query=query.Where(x => SearchPredicate(x, SearchText));

        // FILTER
        if(FilterPredicate!=null)
            query=query.Where(FilterPredicate);

        _filtered=query.ToList();

        TotalPages=Math.Max(1, (int)Math.Ceiling(_filtered.Count/(double)PageSize));
        CurrentPage=Math.Clamp(CurrentPage, 1, TotalPages);

        Project();
    }

    // ================= PROJECTION =================
    private void Project()
    {
        var pageItems = _filtered
            .Skip((CurrentPage-1)*PageSize)
            .Take(PageSize)
            .ToList();

        PagedItems=new ObservableCollection<T>(pageItems);

        if(RowMapper!=null)
            Rows=new ObservableCollection<SparkGridRow>(
                pageItems.Select(RowMapper)
            );

        OnPropertyChanged(nameof(PagedItems));
        OnPropertyChanged(nameof(Rows));
    }
}