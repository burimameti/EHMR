using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;

    public EncounterQuickPreviewPage(ISelectedItemService<Encounter> selectedItemService)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext = _selectedItemService.SelectedItem;
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
#if WINDOWS
        try
        {
            if (PrintWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 nativeWebView)
            {
                await nativeWebView.EnsureCoreWebView2Async();
                await nativeWebView.CoreWebView2.ShowPrintUIAsync();
                return;
            }

            await DisplayAlert("Печатење", "Прегледот за печатење сè уште се подготвува. Обидете се повторно.", "ОК");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await DisplayAlert("Печатење", "Не успеа да се отвори дијалогот за печатење.", "ОК");
        }
#else
        await DisplayAlert("Печатење", "Печатењето е достапно во Windows десктоп верзијата.", "ОК");
#endif
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}