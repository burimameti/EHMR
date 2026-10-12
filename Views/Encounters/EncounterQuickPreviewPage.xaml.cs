using EHMR.Domain.Entities;
using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Views.Encounters;

public partial class EncounterQuickPreviewPage : ContentPage
{
    private readonly ISelectedItemService<Encounter> _selectedItemService;
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;
    private TaskCompletionSource<bool>? _printDocumentLoaded;
    private bool _isPrintDocumentReady;

    public EncounterQuickPreviewPage(ISelectedItemService<Encounter> selectedItemService, IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        InitializeComponent();
        _selectedItemService = selectedItemService;
        _factory = factory;
        PrintWebView.Navigated += OnPrintWebViewNavigated;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var selectedEncounter = _selectedItemService.SelectedItem ?? BindingContext as Encounter;
        var encounter = selectedEncounter;

        _isPrintDocumentReady = false;
        _printDocumentLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        PrintWebView.Source = new HtmlWebViewSource
        {
            Html = "<!doctype html><html><body style='font-family:Arial,sans-serif;color:#111827;background:#fff;padding:24px'><h3>Се вчитува извештајот...</h3></body></html>"
        };

        if (encounter is not null)
        {
            try
            {
                await using (var db = await _factory.CreateDbContextAsync())
                {
                    encounter = await db.Encounters.AsNoTracking()
                        .Include(x => x.Patient).ThenInclude(x => x.Doctor).ThenInclude(x => x.User)
                        .Include(x => x.Doctor).ThenInclude(x => x.User)
                        .FirstOrDefaultAsync(x => x.Id == encounter.Id) ?? encounter;
                }

                BindingContext = encounter;
                var html = await BuildReportHtmlAsync(encounter.PatientId, encounter.Id);
                PrintWebView.Source = new HtmlWebViewSource { Html = html };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encounter preview load failed: {ex}");
                PrintWebView.Source = new HtmlWebViewSource
                {
                    Html = $"<!doctype html><html><body style='font-family:Arial,sans-serif;color:#111827;background:#fff;padding:24px'><h3>Не може да се прикаже извештајот.</h3><pre style='white-space:pre-wrap'>{System.Net.WebUtility.HtmlEncode(ex.Message)}</pre></body></html>"
                };
            }
        }
        else
        {
            PrintWebView.Source = new HtmlWebViewSource
            {
                Html = "<!doctype html><html><body style='font-family:Arial,sans-serif;color:#111827;background:#fff;padding:24px'><h3>Не е избран преглед.</h3><p>Затворете го прегледот и отворете го повторно од листата на прегледи.</p></body></html>"
            };
        }
    }

    private async Task<string> BuildReportHtmlAsync(
        Guid patientId,
        Guid? encounterId = null,
        Guid? appointmentId = null,
        string? title = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var patient = await db.Patients.AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == patientId)
            ?? throw new InvalidOperationException("Пациентот не е пронајден.");

        var encounter = encounterId.HasValue
            ? await db.Encounters.AsNoTracking().Include(x => x.Doctor).ThenInclude(x => x.User)
                .Include(x => x.Appointment)
                .FirstOrDefaultAsync(x => x.Id == encounterId.Value && x.PatientId == patientId)
            : null;
        if (encounterId.HasValue && encounter is null)
            throw new InvalidOperationException("Прегледот не е пронајден.");

        var diagnoses = await db.PatientMkb10Assignments.AsNoTracking().Include(x => x.Mkb10Code)
            .Where(x => x.PatientId == patientId && (encounter == null
                ? x.EncounterId == null : x.EncounterId == encounter.Id))
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Mkb10Code!.Code).ToListAsync();

        var allMedicines = await db.PatientMedicines.AsNoTracking()
            .Include(x => x.Medicine).Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId == patientId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        var encounterMedicines = encounter == null
            ? allMedicines.Where(x => x.EncounterId == null && x.IsActive).Take(5).ToList()
            : allMedicines.Where(x => x.EncounterId == encounter.Id).Take(5).ToList();
        var medicineHistory = allMedicines.Where(x => encounter == null || x.EncounterId != encounter.Id).Take(5).ToList();
        var prescriptions = encounter == null ? new List<Domain.Entities.Prescription>()
            : await db.Prescriptions.AsNoTracking()
                .Where(x => x.PatientId == patientId && x.EncounterId == encounter.Id)
                .OrderBy(x => x.Medication).ToListAsync();
        var scores = await db.PatientScores.AsNoTracking().Where(x => x.PatientId == patientId)
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();
        var previous = encounter == null ? new List<Domain.Entities.Encounter>()
            : await db.Encounters.AsNoTracking().Include(x => x.Doctor).ThenInclude(x => x.User)
                .Where(x => x.PatientId == patientId && x.Id != encounter.Id &&
                    (x.ScheduledStart ?? x.EncounterDate) < (encounter.ScheduledStart ?? encounter.EncounterDate))
                .OrderByDescending(x => x.ScheduledStart ?? x.EncounterDate).Take(5).ToListAsync();
        var next = encounter == null ? null
            : await db.Appointments.AsNoTracking().Include(x => x.Doctor).ThenInclude(x => x.User)
                .Where(x => x.PatientId == patientId && x.Status == Domain.Entities.AppointmentStatus.Scheduled &&
                    x.ScheduledStart > (encounter.ScheduledStart ?? encounter.EncounterDate))
                .OrderBy(x => x.ScheduledStart).FirstOrDefaultAsync();

        static string E(string? value) => System.Net.WebUtility.HtmlEncode(
            string.IsNullOrWhiteSpace(value) ? "—" : value);
        static string SectionHtml(string heading, string body) =>
            $"<section><h2>{E(heading)}</h2>{body}</section>";
        static string Field(string label, string? value) =>
            $"<div class='field'><span class='field-label'>{E(label)}</span><span class='field-value'>{E(value)}</span></div>";
        static string RowHtml(string heading, string? detail = null) =>
            $"<div class='row'><div class='row-title'>{E(heading)}</div>{(string.IsNullOrWhiteSpace(detail) ? "" : $"<div class='row-detail'>{E(detail)}</div>")}</div>";
        static string EmptyHtml(string value) => $"<p class='empty'>{E(value)}</p>";

        var html = new System.Text.StringBuilder();
        var reportTitle = encounter != null ? $"Извештај за преглед бр. {encounter.EncounterNumber}"
            : (string.IsNullOrWhiteSpace(title) ? "Извештај за пациент" : title.Trim());
        html.Append("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>");
        html.Append("@page{size:A4;margin:14mm 15mm 16mm}*{box-sizing:border-box}html,body{background:#fff}body{font-family:Arial,'Segoe UI',sans-serif;color:#111827;margin:0 auto;padding:0;max-width:190mm;font-size:10pt;line-height:1.35}.header{display:flex;justify-content:space-between;gap:18px;align-items:center;border-bottom:1px solid #111827;padding:0 0 7px;margin-bottom:7px}.org{font-size:10pt;font-weight:700;letter-spacing:.2px;color:#64748b}h1{font-size:12pt;margin:0;color:#111827;font-weight:700}.meta{text-align:right;font-size:9pt;color:#111827;line-height:1.45}h2{font-size:10pt;color:#111827;font-weight:700;margin:10px 0 4px;padding:0 0 3px;border-bottom:1px solid #111827;letter-spacing:0}section{break-inside:auto}.grid{display:grid;grid-template-columns:1fr 1fr;column-gap:18px;row-gap:0}.field{display:grid;grid-template-columns:38% 62%;gap:5px;padding:2px 0;border-bottom:1px solid #e5e7eb;break-inside:avoid;min-width:0}.field-label{font-weight:700;font-size:9pt;color:#111827}.field-value{font-size:9pt;color:#111827;overflow-wrap:anywhere}.row{padding:3px 0;border-bottom:1px solid #e5e7eb;break-inside:avoid}.row-title{font-weight:700;font-size:9.5pt;color:#111827}.row-detail{font-size:9pt;color:#111827;margin-top:1px;overflow-wrap:anywhere}.empty{color:#475569;font-size:9pt;margin:3px 0}.signatures{display:flex;gap:48px;margin-top:30px;break-inside:avoid}.sig{flex:1;border-top:1px solid #64748b;padding-top:5px;text-align:center;font-size:9pt;color:#475569}@media(max-width:600px){body{padding:14px}.grid{grid-template-columns:1fr}.header{align-items:flex-start;flex-direction:column}.meta{text-align:left}.field{grid-template-columns:40% 60%}}");
        html.Append("</style></head><body>");
        html.Append($"<div class='header'><div><div class='org'>EHMR</div><h1>{E(reportTitle)}</h1></div><div class='meta'><div>Број: <b>{E(encounter?.EncounterNumber)}</b></div><div>Датум: <b>{E(encounter?.EncounterDate.ToString("dd.MM.yyyy HH:mm"))}</b></div></div></div>");

        var doctorName = encounter?.Doctor?.FullName ?? patient.Doctor?.FullName;
        var fields = new System.Text.StringBuilder("<div class='grid'>");
        fields.Append(Field("Пациент", patient.FullName)).Append(Field("СЗБО", patient.SzboNumber))
            .Append(Field("Датум на раѓање", patient.BirthDate.ToString("dd.MM.yyyy")))
            .Append(Field("Пол", patient.Gender.ToString())).Append(Field("Телефон", patient.Phone))
            .Append(Field("Адреса", JoinAddress(patient.Address, patient.City, patient.PostalCode)))
            .Append(Field("Реуматолог", doctorName));
        if (encounter != null) fields.Append(Field("Датум на преглед", encounter.EncounterDate.ToString("dd.MM.yyyy HH:mm")));
        fields.Append("</div>");
        html.Append(SectionHtml("ОСНОВНИ ПОДАТОЦИ", fields.ToString()));

        if (encounter != null)
        {
            var details = new System.Text.StringBuilder("<div class='grid'>");
            details.Append(Field("Број на преглед", encounter.EncounterNumber))
                .Append("</div>");
            html.Append(SectionHtml("ПОДАТОЦИ ЗА ПРЕГЛЕДОТ", details.ToString()));

            var dx = diagnoses.Count == 0 ? EmptyHtml("Нема внесена МКБ-10 дијагноза за овој преглед.")
                : string.Concat(diagnoses.Select(d => RowHtml($"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}", d.IsPrimary ? "Примарна дијагноза" : null)));
            html.Append(SectionHtml("ДИЈАГНОЗА ЗА ОВОЈ ПРЕГЛЕД (МКБ-10)", dx));

            var meds = new System.Text.StringBuilder();
            foreach (var m in encounterMedicines)
                meds.Append(RowHtml(m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек", BuildMedicineDetails(m)));
            foreach (var p in prescriptions)
                meds.Append(RowHtml(p.Medication ?? "Рецепт", string.Join(" · ", new[] { p.Dosage, p.Instructions, p.Status }.Where(x => !string.IsNullOrWhiteSpace(x)))));
            html.Append(SectionHtml("ЛЕКОВИ ПОВРЗАНИ СО ОВОЈ ПРЕГЛЕД",
                meds.Length == 0 ? EmptyHtml("Нема лекови поврзани со овој преглед.") : meds.ToString()));

            if (next != null)
            {
                var nextFields = "<div class='grid'>" + Field("Датум на преглед", next.ScheduledStart.ToString("dd.MM.yyyy")) +
                    Field("Матичен Реуматолог", next.Doctor?.FullName) +
                    Field("Статус", next.Status.ToString()) + "</div>";
                html.Append(SectionHtml("ПОДАТОЦИ ЗА СЛЕДНА КОНТРОЛА", nextFields));
            }

            var prevHtml = previous.Count == 0 ? EmptyHtml("Нема претходни прегледи.")
                : string.Concat(previous.Select(p => RowHtml($"Преглед бр. {p.EncounterNumber}",
                    $"{(p.ScheduledStart ?? p.EncounterDate):dd.MM.yyyy HH:mm} · {p.Doctor?.FullName ?? "—"} · {p.Status}")));
            html.Append(SectionHtml("ИСТОРИЈА НА ПРЕТХОДНИ ПРЕГЛЕДИ (ДО 5)", prevHtml));
        }

        var scoreHtml = scores.Count == 0 ? EmptyHtml("Нема внесени скорови.")
            : string.Concat(scores.Select(s => RowHtml($"{s.ScoreText} — {(string.IsNullOrWhiteSpace(s.Number) ? "—" : s.Number)}",
                s.RecordedAt.ToLocalTime().ToString("dd.MM.yyyy"))));
        html.Append(SectionHtml("ИСТОРИЈА НА ПОСЛЕДНИ 5 СКОРОВИ", scoreHtml));

        var historyHtml = medicineHistory.Count == 0 ? EmptyHtml("Нема претходни лекови.")
            : string.Concat(medicineHistory.Select(m => RowHtml(m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек", BuildMedicineDetails(m))));
        html.Append(SectionHtml("ИСТОРИЈА НА ЛЕКОВИ (ДО 5)", historyHtml));
        html.Append("<div class='signatures'><div class='sig'>Потпис на лекар</div><div class='sig'>Печат на установа</div></div></body></html>");
        return html.ToString();
    }


    private static string BuildMedicineDetails(Domain.Entities.PatientMedicine medicine)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(medicine.Dosage)) parts.Add($"Доза: {medicine.Dosage}");
        if (medicine.Quantity > 0) parts.Add($"Количина: {medicine.Quantity.ToString("0.################", System.Globalization.CultureInfo.InvariantCulture)}");
        if (!string.IsNullOrWhiteSpace(medicine.ApplicationRegime?.Regime)) parts.Add($"Режим: {medicine.ApplicationRegime.Regime}");
        return string.Join(" · ", parts);
    }

    private static string JoinAddress(string? address, string? city, string? postal)
        => string.Join(", ", new[] { address, city, postal }.Where(x => !string.IsNullOrWhiteSpace(x)));

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
