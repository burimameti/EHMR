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
        var encounter = _selectedItemService.SelectedItem;
        BindingContext = encounter;

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
body{{font-family:Arial,sans-serif;color:#172033;margin:32px;font-size:12pt}}
h1{{font-size:20pt;margin-bottom:4px}}h2{{font-size:12pt;border-bottom:1px solid #cbd5e1;padding-bottom:6px;margin-top:24px}}
.muted{{color:#64748b;font-size:9pt}}.row{{margin:10px 0}}.label{{color:#64748b;font-size:9pt;display:block;margin-bottom:3px}}
.value{{font-weight:600}}.section{{margin-top:18px}}@page{{size:A4;margin:18mm}}
</style></head><body>
<div class='muted'>МЕДИЦИНСКА УСТАНОВА</div><h1>Клинички извештај за преглед</h1>
<div class='muted'>Број: {Safe(encounter.EncounterNumber)} · Датум: {Safe(encounter.EncounterDate.ToString("dd.MM.yyyy"))}</div>
<h2>Податоци за пациентот</h2>
<div class='row'><span class='label'>Име и презиме</span><span class='value'>{Safe(patient?.FullName)}</span></div>
<div class='row'><span class='label'>ЕМБГ</span><span class='value'>{Safe(maskedId)}</span></div>
<div class='row'><span class='label'>СЗБО број</span><span class='value'>{Safe(patient?.SzboNumber)}</span></div>
<div class='row'><span class='label'>Датум на раѓање</span><span class='value'>{Safe(patient?.BirthDate.ToString("dd.MM.yyyy"))}</span></div>
<div class='row'><span class='label'>Реуматолог</span><span class='value'>{Safe($"{doctor?.FirstName} {doctor?.LastName}".Trim())}</span></div>
<h2>Податоци за прегледот</h2>
<div class='row'><span class='label'>Статус</span><span class='value'>{Safe(encounter.Status.ToString())}</span></div>
<div class='section'><b>Главна поплака</b><p>{Safe(encounter.ChiefComplaint)}</p></div>
<div class='section'><b>Анамнеза на сегашната состојба</b><p>{Safe(encounter.HistoryOfPresentIllness)}</p></div>
<div class='section'><b>Процена / дијагностички заклучок</b><p>{Safe(encounter.Assessment)}</p></div>
<div class='section'><b>План и препораки</b><p>{Safe(encounter.Plan)}</p></div>
<div class='section'><b>Клинички забелешки</b><p>{Safe(encounter.ClinicalNotes)}</p></div>
<br><br><table style='width:100%;margin-top:36px'><tr><td style='width:50%;border-top:1px solid #94a3b8;padding-top:8px'>Потпис на лекар</td><td style='width:50%;border-top:1px solid #94a3b8;padding-top:8px'>Печат на установа</td></tr></table>
</body></html>";
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