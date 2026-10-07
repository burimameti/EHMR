using EHMR.ViewModels;
using EHMR.ViewModels.Encounters;
using Microsoft.Maui.Controls;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EHMR.Views.Encounters;

public partial class EncounterCreatePage : ContentPage
{
    private readonly EncounterCreateViewModel _viewModel;
    private readonly MenuView _menu;
    public EncounterCreatePage(EncounterCreateViewModel viewModel, MenuView menu)
    {
        InitializeComponent();
        _viewModel=viewModel;
        _menu=menu;
        BindingContext=_viewModel;
        MenuHost.Content=_menu;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if(_viewModel!=null)
        {
            await _viewModel.LoadAsync();
        }
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

}