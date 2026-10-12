using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IPatientClinicalReportService _clinicalReportService;
    private string? _reportHtml;
    private bool _loading;

    public EncounterQuickPreviewPage(
        ISelectedItemService<Encounter> selectedItemService,
        IPatientClinicalReportService clinicalReportService)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
        _clinicalReportService = clinicalReportService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_loading) return;

        _loading = true;
        try
        {
            var encounter = _selectedItemService.SelectedItem;
            if (encounter is null)
                throw new InvalidOperationException("Не е избран преглед за прикажување.");
            if (encounter.PatientId == Guid.Empty)
                throw new InvalidOperationException("Прегледот нема поврзан пациент.");

            // Use the MAUI WebView source pipeline; bypassing it with a direct
            // WebView2 NavigateToString call can race the handler/navigation.
            _reportHtml = await _clinicalReportService.GenerateHtmlAsync(
                encounter.PatientId,
                encounter.Id,
                title: $"Извештај за преглед - {encounter.EncounterNumber}");

            if (string.IsNullOrWhiteSpace(_reportHtml) ||
                !_reportHtml.Contains("<body", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Сервисот не генерираше валиден HTML извештај.");

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PrintWebView.Opacity = 1;
                PrintWebView.IsVisible = true;
                PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml };
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report preview failed: {ex}");
            var message = System.Net.WebUtility.HtmlEncode(ex.Message);
            var errorHtml = $"<!doctype html><html><head><meta charset='utf-8'></head><body style='font:15px Arial;color:#991b1b;background:#fff;padding:24px'><h2>Извештајот не може да се прикаже</h2><p>{message}</p></body></html>";
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PrintWebView.Opacity = 1;
                PrintWebView.IsVisible = true;
                PrintWebView.Source = new HtmlWebViewSource { Html = errorHtml };
            });
            await DisplayAlert("Извештај", $"Извештајот не може да се прикаже: {ex.Message}", "ОК");
        }
        finally
        {
            _loading = false;
        }
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
#if WINDOWS
        var printButton = sender as VisualElement;
        if (printButton is not null) printButton.IsEnabled = false;
        try
        {
            if (string.IsNullOrWhiteSpace(_reportHtml))
            {
                await DisplayAlert("Печатење", "Извештајот сè уште не е подготвен.", "ОК");
                return;
            }

            if (PrintWebView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 nativeWebView)
            {
                await DisplayAlert("Печатење", "Прегледот за печатење сè уште се иницијализира. Обидете се повторно.", "ОК");
                return;
            }

            await nativeWebView.EnsureCoreWebView2Async();
            var coreWebView = nativeWebView.CoreWebView2;
            if (coreWebView is null)
                throw new InvalidOperationException("Windows WebView не успеа да се иницијализира.");

            // Print the already displayed document. Do not navigate/reload it
            // immediately before opening the print dialog.
            coreWebView.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.System);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report printing failed: {ex}");
            await DisplayAlert("Печатење", $"Не успеа да се отвори дијалогот за печатење: {ex.Message}", "ОК");
        }
        finally
        {
            if (printButton is not null) printButton.IsEnabled = true;
        }
#else
        await DisplayAlert("Печатење", "Печатењето е достапно во Windows десктоп верзијата.", "ОК");
#endif
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
