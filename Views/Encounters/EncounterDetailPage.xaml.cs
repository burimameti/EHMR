using EHMR.ViewModels.Encounters;
using EHMR.Views;
namespace EHMR.Views.Encounters;
[QueryProperty(nameof(EncounterId), "EncounterId")]
public partial class EncounterDetailPage : ContentPage
{
    private readonly EncounterDetailViewModel _viewModel;

    // Create a property to hold the incoming string ID
    private string _encounterId;
    public string EncounterId
    {
        get => _encounterId;
        set => _encounterId=Uri.UnescapeDataString(value??string.Empty);
    }

    public EncounterDetailPage(EncounterDetailViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _viewModel=viewModel;
        BindingContext=_viewModel;
        MenuHost.Content=menu;
    }
    private CancellationTokenSource _searchCts;
    private async void OnMkbSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if(_viewModel==null)
            return;

        _searchCts?.Cancel();
        _searchCts=new CancellationTokenSource();

        var token = _searchCts.Token;
        var query = e.NewTextValue;

        try
        {
            await Task.Delay(250, token); // debounce

            if(!token.IsCancellationRequested)
            {
                await _viewModel.SearchMkbCommand.ExecuteAsync(query);
            }
        }
        catch(TaskCanceledException)
        {
            // ignore
        }
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(_viewModel!=null)
        {
            // Parse the string ID into a Guid if it exists
            if(Guid.TryParse(EncounterId, out var id))
            {
                // This is EDIT/DETAILS mode
                await _viewModel.InitializeAsync(id);
            }
            else
            {
                // This is CREATE mode (ID is null/empty)
                await _viewModel.LoadAsync(id);
            }
        }
    }
}