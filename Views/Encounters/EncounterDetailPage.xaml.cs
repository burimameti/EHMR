using EHMR.ViewModels.Encounters;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace EHMR.Views.Encounters;
[QueryProperty(nameof(EncounterId), "EncounterId")]
public partial class EncounterDetailPage : ContentPage
{
    private readonly EncounterDetailViewModel _viewModel;

    // Make _encounterId nullable to satisfy CS8618
    private string? _encounterId;
    public string EncounterId
    {
        get => _encounterId ?? string.Empty;
        set => _encounterId = Uri.UnescapeDataString(value ?? string.Empty);
    }

    public EncounterDetailPage(EncounterDetailViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        MenuHost.Content = menu;
    }

    // Make _searchCts nullable to satisfy CS8618
    private CancellationTokenSource? _searchCts;
    private async void OnMkbSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_viewModel == null)
            return;

        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;
        var query = e.NewTextValue;

        try
        {
            await Task.Delay(250, token); // debounce

            if (!token.IsCancellationRequested)
            {
                await _viewModel.SearchMkbCommand.ExecuteAsync(query);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is EncounterDetailViewModel vm)
        {
            await vm.LoadAsync();
        }
    }
}