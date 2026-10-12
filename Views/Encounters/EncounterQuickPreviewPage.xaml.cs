using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IPatientClinicalReportService _reportService;
    private TaskCompletionSource<bool>? _printDocumentLoaded;
    private bool _isPrintDocumentReady;

    public EncounterQuickPreviewPage(
        ISelectedItemService<Encounter> selectedItemService,
        IPatientClinicalReportService reportService)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
        _reportService = reportService;
        PrintWebView.Navigated += OnPrintWebViewNavigated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var encounter = _selectedItemService.SelectedItem;
        BindingContext = encounter;

        _isPrintDocumentReady = false;
        _printDocumentLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (encounter is null)
        {
            PrintWebView.Source = new HtmlWebViewSource
            {
                Html = "<!doctype html><html><body style='font-family:Arial;padding:24px;color:#111827'>Прегледот не е избран.</body></html>"
            };
            return;
        }

        try
        {
            var html = await _reportService.GenerateHtmlAsync(
                encounter.PatientId,
                encounter.Id,
                encounter.AppointmentId,
                $"Извештај за преглед бр. {encounter.EncounterNumber}");

            PrintWebView.Source = new HtmlWebViewSource { Html = html };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Encounter report preview failed: {ex}");
            PrintWebView.Source = new HtmlWebViewSource
            {
                Html = "<!doctype html><html><body style='font-family:Arial;padding:24px;color:#991b1b'>Извештајот не можеше да се вчита. Затворете го прегледот и обидете се повторно.</body></html>"
            };
        }
    }

    private void OnPrintWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _isPrintDocumentReady = e.Result == WebNavigationResult.Success;
        _printDocumentLoaded?.TrySetResult(_isPrintDocumentReady);
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
#if WINDOWS
        try
        {
            if (_printDocumentLoaded is not null && !_isPrintDocumentReady)
            {
                var completed = await Task.WhenAny(_printDocumentLoaded.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                if (completed != _printDocumentLoaded.Task || !await _printDocumentLoaded.Task)
                {
                    await DisplayAlert("Печатење", "Извештајот сè уште не е подготвен за печатење. Обидете се повторно.", "ОК");
                    return;
                }
            }

            if (PrintWebView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 nativeWebView)
            {
                await DisplayAlert("Печатење", "Компонентата за печатење не е иницијализирана. Затворете го прегледот и отворете го повторно.", "ОК");
                return;
            }

            await nativeWebView.EnsureCoreWebView2Async();
            if (nativeWebView.CoreWebView2 is null)
            {
                await DisplayAlert("Печатење", "Не можеше да се иницијализира печатењето.", "ОК");
                return;
            }

            nativeWebView.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Encounter report printing failed: {ex}");
            await DisplayAlert("Печатење", $"Не успеа да се отвори дијалогот за печатење: {ex.Message}", "ОК");
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
