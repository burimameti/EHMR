using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels;
using EHMR.Views.Popups;
using Microsoft.EntityFrameworkCore;
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

    private async Task OpenAddScoreAsync()
    {
        if(BindingContext is not ScoreEditorViewModel editor || !editor.IsEditable)
            return;

        var factory=MauiProgram.ServiceProvider.GetRequiredService<IDbContextFactory<DesktopTherapyDbContext>>();
        var popupViewModel=new ScoreEditorViewModel(factory) { IsReadOnly=false };
        var popup=new AddPatientScorePopup(popupViewModel);
        var page=Shell.Current?.CurrentPage;
        if(page is null)
        {
            popupViewModel.Dispose();
            return;
        }

        try
        {
            var result=await page.ShowPopupAsync(popup);
            if(result is ScoreRow row)
                editor.Items.Insert(0,row);
        }
        finally
        {
            popupViewModel.Dispose();
        }
    }
}
