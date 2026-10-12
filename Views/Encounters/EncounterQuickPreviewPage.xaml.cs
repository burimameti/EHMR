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
        var doctorName = $"{doctor?.FirstName} {doctor?.LastName}".Trim();
        var address = string.Join(", ", new[] { patient?.Address, patient?.City, patient?.PostalCode }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

        static string Field(string label, string? value) =>
            $"<div class='field'><span class='label'>{System.Net.WebUtility.HtmlEncode(label)}</span><span class='value'>{Safe(value)}</span></div>";
        static string Section(string title, string body) =>
            $"<section><h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>{body}</section>";
        static string TextBlock(string title, string? value) =>
            $"<div class='text-block'><div class='block-title'>{System.Net.WebUtility.HtmlEncode(title)}</div><div class='block-value'>{Safe(value)}</div></div>";

        var patientFields = new System.Text.StringBuilder("<div class='grid'>");
        patientFields.Append(Field("Пациент", patient?.FullName))
            .Append(Field("СЗБО", patient?.SzboNumber))
      
            .Append(Field("Датум на раѓање", patient?.BirthDate.ToString("dd.MM.yyyy")))
            .Append(Field("Пол", patient?.Gender.ToString()))
            .Append(Field("Телефон", patient?.Phone))
            .Append(Field("Адреса", address))
            .Append(Field("Реуматолог", doctorName))
            .Append("</div>");

        var encounterFields = new System.Text.StringBuilder("<div class='grid'>");
        encounterFields.Append(Field("Број на преглед", encounter.EncounterNumber))
            .Append(Field("Статус", encounter.Status.ToString()))
            .Append(Field("Причина за посета", encounter.ReasonForVisit ?? encounter.Appointment?.ReasonForVisit))
            .Append(Field("Главна тегоба", encounter.ChiefComplaint))
            .Append("</div>");

        return $@"<!doctype html><html><head><meta charset='utf-8'>
<style>
@page{{size:A4;margin:14mm 15mm 16mm}}
*{{box-sizing:border-box}}
html,body{{background:#fff}}
body{{font-family:Arial,'Segoe UI',sans-serif;color:#111827;margin:0 auto;padding:0;max-width:190mm;font-size:10pt;line-height:1.35}}
.header{{display:flex;justify-content:space-between;gap:18px;align-items:center;border-bottom:1px solid #111827;padding:0 0 7px;margin-bottom:7px}}
.org{{font-size:10pt;font-weight:700;letter-spacing:.2px;color:#64748b}}
h1{{font-size:12pt;margin:0;color:#111827;font-weight:700}}
.meta{{text-align:right;font-size:9pt;color:#111827;line-height:1.45}}
h2{{font-size:10pt;color:#111827;font-weight:700;margin:10px 0 4px;padding:0 0 3px;border-bottom:1px solid #111827;letter-spacing:0}}
.grid{{display:grid;grid-template-columns:1fr 1fr;column-gap:18px;row-gap:0}}
.field{{display:grid;grid-template-columns:38% 62%;gap:5px;padding:3px 0;border-bottom:1px solid #e5e7eb;break-inside:avoid;min-width:0}}
.label{{font-weight:700;font-size:9pt;color:#111827}}
.value{{font-size:9pt;color:#111827;overflow-wrap:anywhere}}
.text-block{{margin:7px 0 0;break-inside:avoid}}
.block-title{{font-weight:700;font-size:9.5pt;color:#111827;padding-bottom:3px;border-bottom:1px solid #e5e7eb}}
.block-value{{font-size:9pt;color:#111827;padding:4px 0;white-space:pre-wrap;overflow-wrap:anywhere;min-height:16px}}
.signatures{{display:flex;gap:48px;margin-top:30px;break-inside:avoid}}
.sig{{flex:1;border-top:1px solid #64748b;padding-top:5px;text-align:center;font-size:9pt;color:#475569}}
</style></head><body>
<div class='header'>
  <div><div class='org'>EHMR</div><h1>Извештај за преглед</h1></div>
  <div class='meta'><div>Број: <b>{Safe(encounter.EncounterNumber)}</b></div><div>Датум: <b>{Safe(encounter.EncounterDate.ToString("dd.MM.yyyy HH:mm"))}</b></div></div>
</div>
{Section("ОСНОВНИ ИНФОРМАЦИИ", patientFields.ToString())}
{Section("ПОДАТОЦИ ЗА ПРЕГЛЕДОТ", encounterFields.ToString())}
{TextBlock("АНАМНЕЗА", encounter.HistoryOfPresentIllness)}
{TextBlock("ПРОЦЕНКА", encounter.Assessment)}
{TextBlock("ПЛАН И ПРЕПОРАКИ", encounter.Plan)}
{TextBlock("ЗАБЕЛЕШКА", encounter.Notes ?? encounter.ClinicalNotes ?? encounter.Appointment?.ClinicalNotes)}
<div class='signatures'><div class='sig'>Потпис на лекар</div><div class='sig'>Печат на установа</div></div>
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
        try
        {
            if (_printDocumentLoaded is not null && !_isPrintDocumentReady)
            {
                // Wait for the HTML report to finish loading before opening the native print dialog.
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
