using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using EHMR.Infrastructure.Persistence;
using EHMR.ViewModels;
using EHMR.Views.Popups;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
        var mainPage = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page ?? Application.Current?.MainPage;
        if(mainPage is null)
        {
            popupViewModel.Dispose();
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await mainPage.ShowPopupAsync(popup));

            if (popup.ScoreResult is ScoreRow row)
                editor.Items.Insert(0, row);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not open score popup: {ex}");
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                if (page is not null)
                    await page.DisplayAlert("Грешка", "Не може да се отвори прозорецот за додавање скор.", "OK");
            });
        }
        finally
        {
            popupViewModel.Dispose();
        }
    }
}
