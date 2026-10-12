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

        var encounter = _selectedItemService.SelectedItem;
        if (encounter is null)
        {
            await DisplayAlert("Извештај", "Не е избран преглед за прикажување.", "ОК");
            return;
        }

        _loading = true;
        try
        {
            var patientId = encounter.PatientId;
            if (patientId == Guid.Empty)
            {
                await DisplayAlert("Извештај", "Прегледот нема поврзан пациент.", "ОК");
                return;
            }

            _reportHtml = await _clinicalReportService.GenerateHtmlAsync(
                patientId, encounter.Id, title: $"Извештај за преглед - {encounter.EncounterNumber}");
            PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report preview failed: {ex}");
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
        if (sender is VisualElement button) button.IsEnabled = false;
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
            if (sender is VisualElement button) button.IsEnabled = true;
        }
#else
        await DisplayAlert("Печатење", "Печатењето е достапно во Windows десктоп верзијата.", "ОК");
#endif
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
