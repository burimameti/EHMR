using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EHMR.ViewModels;

public partial class ScoreRow : ObservableObject
{
    public Guid Id { get; set; } = Guid.Empty;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private string number = string.Empty;
}

public sealed record ScoreSuggestion(string Text, string Number, int UsageCount);

public partial class ScoreEditorViewModel : ObservableObject, IDisposable
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;
    private readonly List<Guid> _deletedIds = [];
    private CancellationTokenSource _cts = new();
    private bool _suppressSearch;

    public ScoreEditorViewModel(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
        => _dbFactory=dbFactory;

    public ObservableCollection<ScoreRow> Items { get; } = new();
    public ObservableCollection<ScoreSuggestion> Suggestions { get; } = new();
    public ScoreRow? SelectedScore { get; private set; }
    public IReadOnlyList<Guid> DeletedIds => _deletedIds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private bool isReadOnly = true;

    public bool IsEditable => !IsReadOnly;

    [ObservableProperty] private string descriptionText = string.Empty;
    [ObservableProperty] private string numberText = string.Empty;
    [ObservableProperty] private bool showSuggestions;
    [ObservableProperty] private bool hasSuggestions;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private string searchStatusText = string.Empty;
    [ObservableProperty] private bool hasSearchStatus;

    partial void OnSearchStatusTextChanged(string value)
        => HasSearchStatus = !string.IsNullOrWhiteSpace(value);

    public void Load(IEnumerable<PatientScore> scores)
    {
        _deletedIds.Clear();
        Items.Clear();
        foreach(var s in scores.OrderByDescending(x => x.RecordedAt))
            Items.Add(new ScoreRow
            {
                Id=s.Id,
                RecordedAt=s.RecordedAt,
                Description=s.ScoreText,
                Number=s.Number
            });
    }

    partial void OnDescriptionTextChanged(string value)
    {
        if(_suppressSearch) return;
        _=DebounceSearchAsync(value);
    }

    private async Task DebounceSearchAsync(string query)
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts=new CancellationTokenSource();
        var token=_cts.Token;

        if(string.IsNullOrWhiteSpace(query))
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Suggestions.Clear();
                HasSuggestions=false;
                ShowSuggestions=false;
                IsSearching=false;
                SearchStatusText=string.Empty;
            });
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ShowSuggestions=true;
            IsSearching=true;
            SearchStatusText="Пребарување...";
            HasSuggestions=false;
            Suggestions.Clear();
        });

        try
        {
            await Task.Delay(250,token);
            await using var db=await _dbFactory.CreateDbContextAsync(token);
            var q=query.Trim();
            var definitions=await db.Set<ClinicalScoreDefinition>()
                .AsNoTracking()
                .Where(x=>x.IsActive &&
                    ((x.Name!=null && x.Name.Contains(q)) ||
                     (x.Description!=null && x.Description.Contains(q))))
                .OrderBy(x=>x.Name)
                .Take(30)
                .ToListAsync(token);

            var names=definitions.Select(x=>x.Name).ToList();
            var usage=names.Count==0
                ? new Dictionary<string,int>()
                : await db.PatientScores.AsNoTracking()
                    .Where(x=>names.Contains(x.ScoreText))
                    .GroupBy(x=>x.ScoreText)
                    .Select(g=>new { Text=g.Key, Count=g.Count() })
                    .ToDictionaryAsync(x=>x.Text,x=>x.Count,token);

            var result=definitions.Select(x=>new ScoreSuggestion(
                    x.Name,
                    string.Empty,
                    usage.TryGetValue(x.Name,out var count)?count:0))
                .OrderByDescending(x=>x.UsageCount)
                .ThenBy(x=>x.Text)
                .ToList();

            if(token.IsCancellationRequested) return;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if(token.IsCancellationRequested) return;
                Suggestions.Clear();
                foreach(var item in result) Suggestions.Add(item);
                HasSuggestions=Suggestions.Count>0;
                SearchStatusText=HasSuggestions
                    ? string.Empty
                    : "Нема совпаѓања за пребарувањето.";
                IsSearching=false;
                ShowSuggestions=true;
            });
        }
        catch(OperationCanceledException) { }
        catch(Exception ex)
        {
            Debug.WriteLine($"[ScoreEditor] Search failed: {ex}");
            if(token.IsCancellationRequested) return;
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Suggestions.Clear();
                HasSuggestions=false;
                IsSearching=false;
                ShowSuggestions=true;
                SearchStatusText="Пребарувањето не успеа. Проверете ја врската со базата и обидете се повторно.";
            });
        }
    }

    [RelayCommand]
    private async Task AddNewScoreDefinitionAsync()
    {
        if(IsReadOnly) return;

        var name = await Shell.Current.DisplayPromptAsync(
            "Додади нов скор",
            "Внесете го називот на новиот скор:",
            "Зачувај",
            "Откажи",
            "Пример: DAS28");
        if(string.IsNullOrWhiteSpace(name)) return;
        name=name.Trim();

        var description = await Shell.Current.DisplayPromptAsync(
            "Опис на скор",
            "Внесете опис (опционално):",
            "Зачувај",
            "Откажи",
            "Опис на клиничкиот скор");
        if(description is null) return;

        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var duplicate = await db.Set<ClinicalScoreDefinition>()
                .AnyAsync(x => x.IsActive && x.Name.ToLower() == name.ToLower());
            if(duplicate)
            {
                await Shell.Current.DisplayAlert("Постои скор",
                    $"Скорот „{name}“ веќе постои во регистарот. Пребарајте го и изберете го.",
                    "ОК");
                _suppressSearch=true;
                DescriptionText=name;
                _suppressSearch=false;
                await DebounceSearchAsync(name);
                return;
            }

            db.Set<ClinicalScoreDefinition>().Add(new ClinicalScoreDefinition
            {
                Id=Guid.NewGuid(),
                Name=name,
                Description=description.Trim(),
                IsActive=true
            });
            await db.SaveChangesAsync();

            _suppressSearch=true;
            DescriptionText=name;
            _suppressSearch=false;
            NumberText=string.Empty;
            await DebounceSearchAsync(name);
        }
        catch(Exception ex)
        {
            Debug.WriteLine(ex);
            await Shell.Current.DisplayAlert("Грешка",
                $"Новиот скор не може да се зачува: {ex.Message}", "ОК");
        }
    }

    [RelayCommand]
    private void SelectSuggestion(ScoreSuggestion? suggestion)
    {
        if (suggestion is null) return;

        // Selecting a catalog suggestion adds it directly to the grid.
        // The clinical value is entered in the numeric cell of that row.
        if(Items.Any(x=>string.Equals(x.Description,suggestion.Text,StringComparison.OrdinalIgnoreCase)))
        {
            _suppressSearch=true;
            DescriptionText=string.Empty;
            _suppressSearch=false;
            NumberText=string.Empty;
            Suggestions.Clear();
            HasSuggestions=false;
            ShowSuggestions=false;
            SearchStatusText="Овој скор веќе е додаден во табелата.";
            return;
        }

        var selected = new ScoreRow
        {
            Description = suggestion.Text,
            Number = string.Empty,
            RecordedAt = DateTime.UtcNow
        };
        Items.Insert(0, selected);
        SelectedScore = selected;
        OnPropertyChanged(nameof(SelectedScore));
        _suppressSearch = true;
        DescriptionText = string.Empty;
        _suppressSearch = false;
        NumberText = string.Empty;
        Suggestions.Clear();
        HasSuggestions=false;
        ShowSuggestions=false;
        SearchStatusText=string.Empty;
    }

    [RelayCommand]
    private void Add()
    {
        if(IsReadOnly||string.IsNullOrWhiteSpace(DescriptionText)) return;

        Items.Insert(0, new ScoreRow
        {
            Description=DescriptionText.Trim(),
            Number=(NumberText??string.Empty).Trim(),
            RecordedAt=DateTime.UtcNow
        });

        _suppressSearch=true;
        DescriptionText=string.Empty;
        _suppressSearch=false;
        NumberText=string.Empty;
        Suggestions.Clear();
        HasSuggestions=false;
        ShowSuggestions=false;
        SearchStatusText=string.Empty;
    }

    [RelayCommand]
    private void Remove(ScoreRow? row)
    {
        if(IsReadOnly||row==null) return;
        if(row.Id!=Guid.Empty) _deletedIds.Add(row.Id);
        Items.Remove(row);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}