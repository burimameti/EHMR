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
            // Give the WebView a visible, deterministic document immediately;
            // then replace it with the real clinical report once data is ready.
            PrintWebView.Source = new HtmlWebViewSource
            {
                Html = "<!doctype html><html><body style='font:16px Arial;color:#222;padding:24px'>Се подготвува медицинскиот извештај...</body></html>"
            };

            var encounter = _selectedItemService.SelectedItem;
            if (encounter is null)
                throw new InvalidOperationException("Не е избран преглед за прикажување.");

            if (encounter.PatientId == Guid.Empty)
                throw new InvalidOperationException("Прегледот нема поврзан пациент.");

            _reportHtml = await _clinicalReportService.GenerateHtmlAsync(
                encounter.PatientId,
                encounter.Id,
                title: $"Извештај за преглед - {encounter.EncounterNumber}");

#if WINDOWS
            // Navigate the actual WebView2 surface directly. Setting only the
            // MAUI Source can race the native control during Shell navigation.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (PrintWebView.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 native)
                {
                    await native.EnsureCoreWebView2Async();
                    if (native.CoreWebView2 is not null)
                        native.CoreWebView2.NavigateToString(_reportHtml);
                    else
                        PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml };
                }
                else
                {
                    PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml };
                }
            });
#else
            PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml };
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report preview failed: {ex}");
            var message = System.Net.WebUtility.HtmlEncode(ex.Message);
            PrintWebView.Source = new HtmlWebViewSource
            {
                Html = $"<!doctype html><html><body style='font:15px Arial;color:#991b1b;padding:24px'><h2>Извештајот не може да се прикаже</h2><p>{message}</p></body></html>"
            };
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
        VisualElement? printButton = sender as VisualElement;
        if (printButton != null) printButton.IsEnabled = false;
        try
        {
            if (string.IsNullOrWhiteSpace(_reportHtml))
            {
                await DisplayAlert("Печатење", "Извештајот сè уште не е подготвен.", "ОК");
                return;
            }

            if (PrintWebView.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.WebView2 nativeWebView)
            {
                await DisplayAlert("Печатење", "Прегледот за печатење сè уште се иницијализира. Почекајте момент и обидете се повторно.", "ОК");
                return;
            }

            await nativeWebView.EnsureCoreWebView2Async();
            var coreWebView = nativeWebView.CoreWebView2;
            if (coreWebView is null)
            {
                await DisplayAlert("Печатење", "Windows WebView не успеа да се иницијализира.", "ОК");
                return;
            }

            var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnNavigationCompleted(object? _, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
                => loaded.TrySetResult(args.IsSuccess);

            coreWebView.NavigationCompleted += OnNavigationCompleted;
            try
            {
                coreWebView.NavigateToString(_reportHtml);
                var completed = await Task.WhenAny(loaded.Task, Task.Delay(TimeSpan.FromSeconds(15)));
                if (completed != loaded.Task || !await loaded.Task)
                {
                    await DisplayAlert("Печатење", "Извештајот не се вчита успешно. Обидете се повторно.", "ОК");
                    return;
                }
            }
            finally
            {
                coreWebView.NavigationCompleted -= OnNavigationCompleted;
            }

            coreWebView.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.System);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report printing failed: {ex}");
            await DisplayAlert("Печатење", $"Не успеа да се отвори дијалогот за печатење: {ex.Message}", "ОК");
        }
        finally
        {
            if (printButton != null) printButton.IsEnabled = true;
        }
#else
        await DisplayAlert("Печатење", "Печатењето е достапно во Windows десктоп верзијата.", "ОК");
#endif
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
