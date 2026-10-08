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

public sealed record ScoreSuggestion(string Text, int UsageCount);

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
    public IReadOnlyList<Guid> DeletedIds => _deletedIds;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEditable))]
    private bool isReadOnly = true;

    public bool IsEditable => !IsReadOnly;

    [ObservableProperty] private string descriptionText = string.Empty;
    [ObservableProperty] private string numberText = string.Empty;
    [ObservableProperty] private bool showSuggestions;

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
        var token = _cts.Token;

        if(string.IsNullOrWhiteSpace(query))
        {
            Suggestions.Clear();
            ShowSuggestions=false;
            return;
        }

        try
        {
            await Task.Delay(250, token);

            await using var db = await _dbFactory.CreateDbContextAsync(token);
            var all = await db.PatientScores
                .AsNoTracking()
                .Where(x => x.ScoreText!="")
                .GroupBy(x => x.ScoreText)
                .Select(g => new { Text = g.Key, Count = g.Count() })
                .ToListAsync(token);

            var q = query.Trim();
            var result = all
                .Where(x => x.Text.Contains(q, StringComparison.CurrentCultureIgnoreCase))
                .Select(x => new
                {
                    x.Text,
                    x.Count,
                    Exact = string.Equals(x.Text.Trim(), q, StringComparison.CurrentCultureIgnoreCase),
                    StartsWith = x.Text.StartsWith(q, StringComparison.CurrentCultureIgnoreCase)
                })
                // Најдобриот кандидат секогаш е прв:
                // exact match → starts with → contains, па употреба.
                .OrderByDescending(x => x.Exact)
                .ThenByDescending(x => x.StartsWith)
                .ThenByDescending(x => x.Count)
                .ThenBy(x => x.Text)
                .Take(8)
                .Select(x => new ScoreSuggestion(x.Text, x.Count))
                .ToList();

            if(token.IsCancellationRequested) return;

            Suggestions.Clear();
            foreach(var r in result) Suggestions.Add(r);
            ShowSuggestions=Suggestions.Count>0;
        }
        catch(OperationCanceledException) { }
        catch(Exception ex) { Debug.WriteLine(ex); }
    }

    [RelayCommand]
    private void SelectSuggestion(ScoreSuggestion? suggestion)
    {
        if(suggestion==null) return;
        _suppressSearch=true;
        DescriptionText=suggestion.Text;
        _suppressSearch=false;
        Suggestions.Clear();
        ShowSuggestions=false;
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
        ShowSuggestions=false;
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