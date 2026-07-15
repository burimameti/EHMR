

using EHMR.ViewModels.Encounters;

namespace EHMR.Views.Encounters;

public partial class EncounterEditPage : ContentPage
{
    private EncounterEditViewModel _VM;

    public EncounterEditPage(EncounterEditViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _VM=viewModel;
        BindingContext=_VM;
        MenuHost.Content=menu;
    }

    protected override async void OnAppearing()
    {
      
            await _VM.LoadAsync();
      
    }
    private CancellationTokenSource _searchCts;
    private async void OnMkbSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        if(_VM==null)
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
                await _VM.SearchMkbCommand.ExecuteAsync(query);
            }
        }
        catch(TaskCanceledException)
        {
            // ignore
        }
    }
}