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

            BindingContext = encounter;
            _reportHtml = await _clinicalReportService.GenerateHtmlAsync(
                encounter.PatientId,
                encounter.Id,
                title: $"Извештај за преглед - {encounter.EncounterNumber}");

            if (string.IsNullOrWhiteSpace(_reportHtml) ||
                !_reportHtml.Contains("<body", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Сервисот не генерираше валиден HTML извештај.");

            await MainThread.InvokeOnMainThreadAsync(() =>
                PrintWebView.Source = new HtmlWebViewSource { Html = _reportHtml });
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
        try
        {
            if (string.IsNullOrWhiteSpace(_reportHtml))
            {
                await DisplayAlert("Печатење", "Извештајот сè уште не е подготвен.", "ОК");
                return;
            }

            if (PrintWebView.Handler?.PlatformView is not WebView2 nativeWebView)
            {
                await DisplayAlert("Печатење", "Прегледот сè уште не е подготвен. Обидете се повторно.", "ОК");
                return;
            }

            await nativeWebView.EnsureCoreWebView2Async();
            var core = nativeWebView.CoreWebView2
                ?? throw new InvalidOperationException("Windows WebView не успеа да се иницијализира.");
            core.ShowPrintUI(CoreWebView2PrintDialogKind.System);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clinical report printing failed: {ex}");
            await DisplayAlert("Печатење", $"Не успеа да се отвори дијалогот за печатење: {ex.Message}", "ОК");
        }
#else
        await DisplayAlert("Печатење", "Печатењето е достапно во Windows десктоп верзијата.", "ОК");
#endif
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}