using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private TaskCompletionSource<bool>? _printDocumentLoaded;
    private bool _isPrintDocumentReady;

    public EncounterQuickPreviewPage(ISelectedItemService<Encounter> selectedItemService)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
        PrintWebView.Navigated += OnPrintWebViewNavigated;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var encounter = _selectedItemService.SelectedItem;
        BindingContext = encounter;

        _isPrintDocumentReady = false;
        _printDocumentLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (encounter is not null)
            PrintWebView.Source = new HtmlWebViewSource { Html = BuildPrintHtml(encounter) };
    }

    private static string BuildPrintHtml(Encounter encounter)
    {
        static string Safe(string? value) => System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(value) ? "—" : value);

        var patient = encounter.Patient;
        var doctor = encounter.Doctor?.User;
        var nationalId = patient?.NationalId;
        var maskedId = string.IsNullOrWhiteSpace(nationalId) ? "—" :
            nationalId.Length <= 4 ? new string('•', nationalId.Length) :
            new string('•', nationalId.Length - 4) + nationalId[^4..];

        return $@"<!doctype html><html><head><meta charset='utf-8'>
<style>
@page{{size:A4;margin:16mm 16mm 18mm 16mm}}
*{{box-sizing:border-box}}
body{{font-family:'Segoe UI',Arial,sans-serif;color:#172033;margin:0;font-size:10.5pt;line-height:1.5}}

.header{{display:flex;justify-content:space-between;align-items:flex-end;border-bottom:2px solid #0f4c81;padding-bottom:12px;margin-bottom:20px}}
.org{{color:#0f4c81;font-size:8.5pt;font-weight:700;letter-spacing:1.2px;text-transform:uppercase}}
h1{{font-size:19pt;margin:4px 0 0 0;color:#0f172a;font-weight:700}}
.meta{{text-align:right;font-size:9pt;color:#475569;line-height:1.6}}
.meta b{{color:#172033}}

h2{{font-size:10pt;color:#0f4c81;text-transform:uppercase;letter-spacing:.8px;margin:22px 0 10px 0;padding-bottom:5px;border-bottom:1px solid #cbd5e1}}

.grid{{display:grid;grid-template-columns:1fr 1fr;gap:10px 28px}}
.field{{padding:8px 12px;background:#f8fafc;border-left:3px solid #0f4c81;border-radius:2px}}
.label{{display:block;color:#64748b;font-size:8pt;text-transform:uppercase;letter-spacing:.5px;margin-bottom:2px}}
.value{{font-weight:600;font-size:10.5pt;color:#0f172a}}

.block{{margin-bottom:14px;page-break-inside:avoid}}
.block-title{{font-size:9pt;font-weight:700;color:#334155;text-transform:uppercase;letter-spacing:.5px;margin-bottom:4px}}
.block-body{{margin:0;padding:8px 12px;border:1px solid #e2e8f0;border-radius:3px;min-height:34px;white-space:pre-wrap}}

.signatures{{display:flex;gap:48px;margin-top:56px;page-break-inside:avoid}}
.sig{{flex:1;border-top:1px solid #94a3b8;padding-top:6px;font-size:9pt;color:#64748b;text-align:center}}

.footer{{margin-top:28px;font-size:8pt;color:#94a3b8;text-align:center}}
</style></head><body>

<div class='header'>
  <div>
    <div class='org'>Медицинска установа</div>
    <h1>Клинички извештај за преглед</h1>
  </div>
  <div class='meta'>
    <div>Број: <b>{Safe(encounter.EncounterNumber)}</b></div>
    <div>Датум: <b>{Safe(encounter.EncounterDate.ToString("dd.MM.yyyy"))}</b></div>
  </div>
</div>

<h2>Податоци за пациентот</h2>
<div class='grid'>
  <div class='field'><span class='label'>Име и презиме</span><span class='value'>{Safe(patient?.FullName)}</span></div>
  <div class='field'><span class='label'>ЕМБГ</span><span class='value'>{Safe(maskedId)}</span></div>
  <div class='field'><span class='label'>СЗБО број</span><span class='value'>{Safe(patient?.SzboNumber)}</span></div>
  <div class='field'><span class='label'>Датум на раѓање</span><span class='value'>{Safe(patient?.BirthDate.ToString("dd.MM.yyyy"))}</span></div>
  <div class='field'><span class='label'>Реуматолог</span><span class='value'>{Safe($"{doctor?.FirstName} {doctor?.LastName}".Trim())}</span></div>
  <div class='field'><span class='label'>Статус</span><span class='value'>{Safe(encounter.Status.ToString())}</span></div>
</div>

<h2>Податоци за прегледот</h2>
<div class='block'><div class='block-title'>Главна поплака</div><div class='block-body'>{Safe(encounter.ChiefComplaint)}</div></div>
<div class='block'><div class='block-title'>Анамнеза на сегашната состојба</div><div class='block-body'>{Safe(encounter.HistoryOfPresentIllness)}</div></div>
<div class='block'><div class='block-title'>Процена / дијагностички заклучок</div><div class='block-body'>{Safe(encounter.Assessment)}</div></div>
<div class='block'><div class='block-title'>План и препораки</div><div class='block-body'>{Safe(encounter.Plan)}</div></div>
<div class='block'><div class='block-title'>Клинички забелешки</div><div class='block-body'>{Safe(encounter.ClinicalNotes)}</div></div>

<div class='signatures'>
  <div class='sig'>Потпис на лекар</div>
  <div class='sig'>Печат на установа</div>
</div>

</body></html>";
    }

    private void OnPrintWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _isPrintDocumentReady = e.Result == WebNavigationResult.Success;
        _printDocumentLoaded?.TrySetResult(_isPrintDocumentReady);
    }

    private async void OnPrintClicked(object sender, EventArgs e)
    {
#if WINDOWS
        if (sender is Button button)
            button.IsEnabled = false;

        try
        {
            if (_selectedItemService.SelectedItem is not Encounter encounter)
            {
                await DisplayAlert("Печатење", "Не е избран преглед за печатење. Затворете го прегледот и отворете го повторно.", "ОК");
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

            // Navigate through the native WebView2 instance and wait for its
            // navigation event. This avoids relying on MAUI's WebView.Navigated
            // event, which can race with page appearance/handler initialization.
            var loaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnNavigationCompleted(object? _, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
            {
                loaded.TrySetResult(args.IsSuccess);
            }

            coreWebView.NavigationCompleted += OnNavigationCompleted;
            try
            {
                coreWebView.NavigateToString(BuildPrintHtml(encounter));
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
            System.Diagnostics.Debug.WriteLine($"Encounter report printing failed: {ex}");
            await DisplayAlert("Печатење", $"Не успеа да се отвори дијалогот за печатење: {ex.Message}", "ОК");
        }
        finally
        {
            if (sender is Button button)
                button.IsEnabled = true;
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