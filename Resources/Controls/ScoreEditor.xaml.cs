using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels;
using EHMR.Views.Popups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;

namespace EHMR.Resources.Controls;

public partial class ScoreEditor : ContentView
{
    public ScoreEditor()
    {
        InitializeComponent();
        OpenAddScoreCommand=new Command(async () => await OpenAddScoreAsync());
    }

    public ICommand OpenAddScoreCommand { get; }

    private async void OnAddScoreClicked(object? sender, EventArgs e)
        => await OpenAddScoreAsync();

    private async Task OpenAddScoreAsync()
    {
        if(BindingContext is not ScoreEditorViewModel editor || !editor.IsEditable)
            return;

        var factory=MauiProgram.ServiceProvider.GetRequiredService<IDbContextFactory<DesktopTherapyDbContext>>();
        var popupViewModel=new ScoreEditorViewModel(factory) { IsReadOnly=false };
        var popup=new AddPatientScorePopup(popupViewModel);
        if(Shell.Current is null)
        {
            popupViewModel.Dispose();
            return;
        }

        try
        {
            await Shell.Current.CurrentPage.ShowPopupAsync(popup);
            if(popup.ScoreResult is ScoreRow row)
                editor.Items.Insert(0,row);
        }
        finally
        {
            popupViewModel.Dispose();
        }
    }
}
