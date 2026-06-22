using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EHMR.Abstraction;
using EHMR.Infrastructure.Services;

using System.Collections.ObjectModel;

namespace EHMR.ViewModels.Support;

/// <summary>
/// Backs the МКБ-10 import page. Lets the user pick an exported CSV/TSV file, run
/// <see cref="Mkb10ImportService"/> against it, and see the resulting counts/errors.
/// Not registered against Shell routing by default since this is an occasional admin
/// action — open it via <c>Navigation.PushModalAsync</c> (or register a Shell route if you
/// want deep-linking to it) from wherever your "Import codes..." action lives.
///
/// Deliberately does NOT use generated OnXChanged partial hooks for IsImporting/
/// SelectedFilePath/InsertedCount/etc. Different versions of the CommunityToolkit.Mvvm
/// generator have emitted those declarations with slightly different signatures/modifiers
/// across releases, which is exactly what produces CS8799 ("Both partial member
/// declarations must have identical accessibility modifiers") the moment your hand-written
/// implementation doesn't match byte-for-byte. Calling NotifyCanExecuteChanged/
/// OnPropertyChanged explicitly at the point of change is slightly more verbose but has
/// zero dependency on generator internals, so it can't break across a toolkit upgrade.
/// </summary>
public partial class MbkImportExportViewModel : ObservableObject
{
    private readonly Mkb10ImportService _importService;
    private readonly IFileDialogService _fileDialogService;
    private CancellationTokenSource? _cts;

    public MbkImportExportViewModel(Mkb10ImportService importService, IFileDialogService fileDialogService)
    {
        _importService=importService;
        _fileDialogService=fileDialogService;
    }

    [ObservableProperty]
    private string? _selectedFilePath;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private string _statusText = "Изберете датотека за увоз.";

    [ObservableProperty]
    private int _insertedCount;

    [ObservableProperty]
    private int _updatedCount;

    [ObservableProperty]
    private int _skippedCount;

    /// <summary>True once an import has produced any counts or errors, so the results panel
    /// in the view only appears after something has actually run. Raised manually wherever
    /// InsertedCount/UpdatedCount/SkippedCount/Errors change — see ImportAsync below.</summary>
    public bool HasResult => InsertedCount>0||UpdatedCount>0||SkippedCount>0||Errors.Count>0;

    /// <summary>0–1 fraction for MAUI's <c>ProgressBar.Progress</c>, which (unlike WPF's
    /// Minimum/Maximum/Value model) only takes a single fractional double. Raised manually
    /// wherever ProgressCurrent/ProgressTotal change — see the Progress callback below.</summary>
    public double ImportProgressFraction => ProgressTotal>0 ? (double)ProgressCurrent/ProgressTotal : 0d;

    public ObservableCollection<string> Errors { get; } = new();

    private bool CanBrowse() => !IsImporting;

    [RelayCommand(CanExecute = nameof(CanBrowse))]
    private async Task BrowseFileAsync()
    {
        var path = await _fileDialogService.PickOpenFileAsync("Изберете датотека со МКБ-10 кодови");

        if(path is not null)
        {
            SelectedFilePath=path;
            StatusText=$"Избрана датотека: {Path.GetFileName(path)}";
            ClearResults();
            ImportCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanImport() => !string.IsNullOrWhiteSpace(SelectedFilePath)&&!IsImporting;

    [RelayCommand(CanExecute = nameof(CanImport))]
    private async Task ImportAsync()
    {
        if(string.IsNullOrWhiteSpace(SelectedFilePath)) return;

        IsImporting=true;
        NotifyAllCommands();
        ClearResults();
        StatusText="Увозот е во тек...";
        _cts=new CancellationTokenSource();

        var progress = new Progress<Mkb10ImportProgress>(p =>
        {
            ProgressCurrent=p.Processed;
            ProgressTotal=p.Total;
            OnPropertyChanged(nameof(ImportProgressFraction));
        });

        try
        {
            var result = await _importService.ImportAsync(SelectedFilePath, progress, _cts.Token);

            InsertedCount=result.Inserted;
            UpdatedCount=result.Updated;
            SkippedCount=result.Skipped;
            Errors.Clear();
            foreach(var error in result.Errors)
            {
                Errors.Add(error);
            }
            OnPropertyChanged(nameof(HasResult));

            StatusText=Errors.Count==0
                ? "Увозот заврши успешно."
                : "Увозот заврши со предупредувања — видете подолу.";
        }
        catch(OperationCanceledException)
        {
            StatusText="Увозот е прекинат.";
        }
        catch(Exception ex)
        {
            Errors.Add($"Неочекувана грешка: {ex.Message}");
            OnPropertyChanged(nameof(HasResult));
            StatusText="Увозот не успеа.";
        }
        finally
        {
            IsImporting=false;
            NotifyAllCommands();
            _cts=null;
        }
    }

    private bool CanCancel() => IsImporting;

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelImport()
    {
        _cts?.Cancel();
    }

    private void ClearResults()
    {
        InsertedCount=0;
        UpdatedCount=0;
        SkippedCount=0;
        ProgressCurrent=0;
        ProgressTotal=0;
        Errors.Clear();
        OnPropertyChanged(nameof(HasResult));
        OnPropertyChanged(nameof(ImportProgressFraction));
    }

    /// <summary>IsImporting gates all three commands (Browse is disabled while importing,
    /// Import can't be double-triggered, Cancel only makes sense while importing), so every
    /// place IsImporting flips, all three need refreshing together.</summary>
    private void NotifyAllCommands()
    {
        BrowseFileCommand.NotifyCanExecuteChanged();
        ImportCommand.NotifyCanExecuteChanged();
        CancelImportCommand.NotifyCanExecuteChanged();
    }
}