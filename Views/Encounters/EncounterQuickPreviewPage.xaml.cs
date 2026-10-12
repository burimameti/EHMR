using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
#endif

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
            var encounter = _selectedItemService.SelectedItem
                ?? throw new InvalidOperationException("Не е избран преглед за прикажување.");
            if (encounter.PatientId == Guid.Empty)
                throw new InvalidOperationException("Прегледот нема поврзан пациент.");

            _reportHtml = await _clinicalReportService.GenerateHtmlAsync(
                encounter.PatientId,
                encounter.Id,
                title: $"Извештај за преглед - {encounter.EncounterNumber}");

            if (string.IsNullOrWhiteSpace(_reportHtml) ||
                !_reportHtml.Contains("<body", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Сервисот не генерираше валиден HTML извештај.");

#if WINDOWS
            var nativeWebView = await GetNativeWebViewAsync();
            await nativeWebView.EnsureCoreWebView2Async();
            var core = nativeWebView.CoreWebView2
                ?? throw new InvalidOperationException("Windows WebView не успеа да се иницијализира.");

            var navigation = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<CoreWebView2NavigationCompletedEventArgs>? completed = null;
            completed = (_, args) =>
            {
                core.NavigationCompleted -= completed;
                if (args.IsSuccess)
                    navigation.TrySetResult(true);
                else
                    navigation.TrySetException(new InvalidOperationException(
                        $"HTML извештајот не се вчита (WebView2 error: {args.WebErrorStatus})."));
            };

            core.NavigationCompleted += completed;
            core.NavigateToString(_reportHtml);
            await navigation.Task.WaitAsync(TimeSpan.FromSeconds(20));
#else
            await MainThread.InvokeOnMainThreadAsync(() =>
                PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml });
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report preview failed: {ex}");
            var message = System.Net.WebUtility.HtmlEncode(ex.Message);
            var errorHtml = $"<!doctype html><html><head><meta charset='utf-8'></head><body style='font:15px Arial;color:#991b1b;background:#fff;padding:24px'><h2>Извештајот не може да се прикаже</h2><p>{message}</p></body></html>";
#if WINDOWS
            try
            {
                var nativeWebView = await GetNativeWebViewAsync();
                await nativeWebView.EnsureCoreWebView2Async();
                nativeWebView.CoreWebView2?.NavigateToString(errorHtml);
            }
            catch (Exception fallbackException)
            {
                System.Diagnostics.Debug.WriteLine($"Could not display report error page: {fallbackException}");
            }
#else
            await MainThread.InvokeOnMainThreadAsync(() =>
                PrintWebView.Source = new HtmlWebViewSource { Html = errorHtml });
#endif
            await DisplayAlert("Извештај", $"Извештајот не може да се прикаже: {ex.Message}", "ОК");
        }
        finally
        {
            _loading = false;
        }
    }

#if WINDOWS
    private async Task<WebView2> GetNativeWebViewAsync()
    {
        if (PrintWebView.Handler?.PlatformView is WebView2 existing)
            return existing;

        var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handlerChanged = null;
        handlerChanged = (_, _) =>
        {
            if (PrintWebView.Handler?.PlatformView is WebView2)
            {
                PrintWebView.HandlerChanged -= handlerChanged;
                ready.TrySetResult(true);
            }
        };

        PrintWebView.HandlerChanged += handlerChanged;
        if (PrintWebView.Handler?.PlatformView is WebView2)
        {
            PrintWebView.HandlerChanged -= handlerChanged;
            return (WebView2)PrintWebView.Handler.PlatformView;
        }

        try
        {
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            PrintWebView.HandlerChanged -= handlerChanged;
        }

        return PrintWebView.Handler?.PlatformView as WebView2
            ?? throw new InvalidOperationException("Прозорецот за извештај не е подготвен.");
    }
#endif

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

            var nativeWebView = await GetNativeWebViewAsync();
            await nativeWebView.EnsureCoreWebView2Async();
            var core = nativeWebView.CoreWebView2
                ?? throw new InvalidOperationException("Windows WebView не успеа да се иницијализира.");

            // Open the system print dialog for the report already shown on this page.
            core.ShowPrintUI(CoreWebView2PrintDialogKind.System);
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
