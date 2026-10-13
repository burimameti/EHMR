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
                await LoadDynamicReportSectionsAsync(encounter.PatientId, encounter.Id);
                var html = await BuildReportHtmlAsync(encounter.PatientId, encounter.Id);
                _isPrintDocumentReady = false;
                _printDocumentLoaded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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


    private async Task LoadDynamicReportSectionsAsync(Guid patientId, Guid encounterId)
    {
        DynamicReportSections.Children.Clear();
        await using var db = await _factory.CreateDbContextAsync();

        var diagnoses = await db.PatientMkb10Assignments.AsNoTracking()
            .Include(x => x.Mkb10Code)
            .Where(x => x.PatientId == patientId && x.EncounterId == encounterId)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Mkb10Code!.Code)
            .Take(5).ToListAsync();

        var medicines = await db.PatientMedicines.AsNoTracking()
            .Include(x => x.Medicine).Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId == patientId && x.EncounterId == encounterId)
            .OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync();

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.EncounterId == encounterId)
            .OrderBy(x => x.Medication).Take(5).ToListAsync();

        var scores = await db.PatientScores.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.EncounterId == encounterId)
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();

        var recentEncounterIds = await db.Encounters.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.Id != encounterId)
            .OrderByDescending(x => x.ScheduledStart ?? x.EncounterDate)
            .Select(x => x.Id).Take(5).ToListAsync();

        var diagnosisHistory = await db.PatientMkb10Assignments.AsNoTracking()
            .Include(x => x.Mkb10Code).Include(x => x.Encounter)
            .Where(x => x.PatientId == patientId && x.EncounterId.HasValue &&
                        recentEncounterIds.Contains(x.EncounterId.Value))
            .OrderByDescending(x => x.Encounter!.ScheduledStart ?? x.Encounter.EncounterDate)
            .ThenByDescending(x => x.DiagnosedAt).Take(5).ToListAsync();

        var medicineHistory = await db.PatientMedicines.AsNoTracking()
            .Include(x => x.Medicine).Include(x => x.ApplicationRegime).Include(x => x.Encounter)
            .Where(x => x.PatientId == patientId && x.EncounterId.HasValue && x.EncounterId != encounterId)
            .OrderByDescending(x => x.Encounter!.ScheduledStart ?? x.Encounter.EncounterDate)
            .Take(5).ToListAsync();

        var scoreHistory = await db.PatientScores.AsNoTracking()
            .Where(x => x.PatientId == patientId && x.EncounterId != encounterId)
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();

        void AddSection(string title, IEnumerable<(string Main, string? Detail)> rows, string emptyText)
        {
            var section = new VerticalStackLayout { Spacing = 3 };
            section.Children.Add(new Label
            {
                Text = title,
                FontSize = 12,
                TextColor = Color.FromArgb("#111827")
            });
            section.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#111827"), HeightRequest = 1 });
            var rowList = rows.ToList();
            if (rowList.Count == 0)
            {
                section.Children.Add(new Label { Text = emptyText, FontSize = 12, TextColor = Color.FromArgb("#475569") });
            }
            else
            {
                foreach (var item in rowList)
                {
                    var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, Padding = new Thickness(0, 1) };
                    row.Add(new Label { Text = item.Main, FontSize = 12, TextColor = Color.FromArgb("#111827"), LineBreakMode = LineBreakMode.WordWrap });
                    if (!string.IsNullOrWhiteSpace(item.Detail))
                        row.Add(new Label { Text = item.Detail, FontSize = 11, TextColor = Color.FromArgb("#334155"), HorizontalTextAlignment = TextAlignment.End, VerticalTextAlignment = TextAlignment.Center }, 1, 0);
                    section.Children.Add(row);
                    section.Children.Add(new BoxView { BackgroundColor = Color.FromArgb("#E2E8F0"), HeightRequest = 1 });
                }
            }
            DynamicReportSections.Children.Add(section);
        }

        AddSection("ДИЈАГНОЗА (МКБ-10)",
            diagnoses.Select(d => ($"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}", d.IsPrimary ? "" : null)),
            "Нема внесена МКБ-10 дијагноза за овој преглед.");

        var currentMedicineRows = medicines.Select(m =>
            (m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек",
             string.Join(" · ", new[] { m.Dosage, m.ApplicationRegime?.Regime, m.Quantity > 0 ? $"Количина: {m.Quantity:0.##}" : null }.Where(v => !string.IsNullOrWhiteSpace(v)))));
            
            //.Take();
        AddSection("ЛЕКОВИ ВО ОВОЈ ПРЕГЛЕД", currentMedicineRows,
            "Нема лекови поврзани со овој преглед.");

        var currentScoreLine = scores.Count == 0
            ? "Нема внесени скорови за овој преглед."
            : string.Join("   |   ", scores.Select(s => $"{s.ScoreText}: {(string.IsNullOrWhiteSpace(s.Number) ? "—" : s.Number)} ({s.RecordedAt.ToLocalTime():dd.MM.yyyy})"));
        AddSection("СКОР ЗА ОВОЈ ПРЕГЛЕД", new[] { (currentScoreLine, (string?)null) }, "");

        AddSection("ИСТОРИЈА СКОРОВИ",
            scoreHistory.Select(s => ($"{s.ScoreText}: {(string.IsNullOrWhiteSpace(s.Number) ? "—" : s.Number)}", s.RecordedAt.ToLocalTime().ToString("dd.MM.yyyy"))),
            "Нема претходни скорови.");

        AddSection("ИСТОРИЈА НА ЛЕКОВИ ПО ПРЕГЛЕДИ",
            medicineHistory.Select(m => (m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек",
                $"")),
            "Нема претходни лекови поврзани со прегледи.");

        AddSection("ИСТОРИЈА НА ДИЈАГНОЗИ — ПОСЛЕДНИ 5 ПРЕГЛЕДИ",
            diagnosisHistory.Select(d => ($"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}",
                $"")),
            "Нема дијагнози во последните 5 прегледи.");
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
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Mkb10Code!.Code).Take(5).ToListAsync();

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
        var scores = await db.PatientScores.AsNoTracking().Where(x => x.PatientId == patientId &&
                (encounter == null ? x.EncounterId == null : x.EncounterId == encounter.Id))
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();
        var scoreHistory = await db.PatientScores.AsNoTracking()
            .Where(x => x.PatientId == patientId && (encounter == null || x.EncounterId != encounter.Id))
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();
        var recentEncounterIds = encounter == null ? new List<Guid>() :
            await db.Encounters.AsNoTracking().Where(x => x.PatientId == patientId && x.Id != encounter.Id)
                .OrderByDescending(x => x.ScheduledStart ?? x.EncounterDate).Select(x => x.Id).Take(5).ToListAsync();
        var diagnosisHistory = recentEncounterIds.Count == 0 ? new List<PatientMkb10Assignment>() :
            await db.PatientMkb10Assignments.AsNoTracking().Include(x => x.Mkb10Code).Include(x => x.Encounter)
                .Where(x => x.PatientId == patientId && x.EncounterId.HasValue && recentEncounterIds.Contains(x.EncounterId.Value))
                .OrderByDescending(x => x.Encounter!.ScheduledStart ?? x.Encounter.EncounterDate)
                .ThenByDescending(x => x.DiagnosedAt).Take(5).ToListAsync();
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
        var reportTitle = encounter != null ? $"БРОЈ НА ПРЕГЛЕД: {encounter.EncounterNumber}"
            : (string.IsNullOrWhiteSpace(title) ? "Извештај за пациент" : title.Trim());
        html.Append("<!doctype html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><style>");
        html.Append("@page{size:A4;margin:14mm 15mm 16mm}*{box-sizing:border-box}html,body{background:#fff}body{font-family:Arial,'Segoe UI',sans-serif;color:#111827;margin:0 auto;padding:0;max-width:190mm;font-size:12pt;line-height:1.2;font-weight:400}.header{display:flex;justify-content:space-between;gap:12px;align-items:center;border-bottom:1px solid #111827;padding:0 0 4px;margin-bottom:4px}.org{font-size:12pt;font-weight:400;letter-spacing:0;color:#111827}h1{font-size:14pt;margin:0;color:#111827;font-weight:400}.meta{text-align:right;font-size:12pt;color:#111827;line-height:1.2;font-weight:400}h2{font-size:12pt;color:#111827;font-weight:400;margin:5px 0 2px;padding:0 0 2px;border-bottom:1px solid #111827;letter-spacing:0}section{break-inside:auto}.grid{display:grid;grid-template-columns:1fr 1fr;column-gap:18px;row-gap:0}.field{display:grid;grid-template-columns:38% 62%;gap:4px;padding:1px 0;border-bottom:1px solid #e5e7eb;break-inside:avoid;min-width:0}.field-label{font-weight:400;font-size:12pt;color:#111827}.field-value{font-size:12pt;font-weight:400;color:#111827;overflow-wrap:anywhere}.row{padding:1px 0;border-bottom:1px solid #e5e7eb;break-inside:avoid}.row-title{font-weight:400;font-size:12pt;color:#111827}.row-detail{font-size:12pt;font-weight:400;color:#111827;margin-top:0;overflow-wrap:anywhere}.empty{color:#475569;font-size:12pt;margin:1px 0}.signatures{display:flex;gap:32px;margin-top:16px;break-inside:avoid}.sig{flex:1;border-top:1px solid #64748b;padding-top:3px;text-align:center;font-size:12pt;font-weight:400;color:#475569}@media(max-width:600px){body{padding:14px}.grid{grid-template-columns:1fr}.header{align-items:flex-start;flex-direction:column}.meta{text-align:left}.field{grid-template-columns:40% 60%}}");
        html.Append("</style></head><body>");
        html.Append($"<div class='header'><div><div class='org'>КЛИНИКА ЗА РЕУМАТОЛОГИЈА -СКОПЈЕ</div><h1>{E(reportTitle)}</h1></div><div class='meta'><div>Датум: <b>{E(encounter?.EncounterDate.ToString("dd.MM.yyyy HH:mm"))}</b></div></div></div>");

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
            var dx = diagnoses.Count == 0 ? EmptyHtml("Нема внесена МКБ-10 дијагноза за овој преглед.")
                : string.Concat(diagnoses.Select(d => RowHtml($"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}", d.IsPrimary ? "Примарна дијагноза" : null)));
            html.Append(SectionHtml("ДИЈАГНОЗА ЗА ОВОЈ ПРЕГЛЕД (МКБ-10)", dx));

            var meds = new System.Text.StringBuilder();
            foreach (var m in encounterMedicines)
                meds.Append(RowHtml(m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек", BuildMedicineDetails(m)));
  
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

        var currentScoreHtml = scores.Count == 0 ? EmptyHtml("Нема внесени скорови за овој преглед.")
            : RowHtml(string.Join("   |   ", scores.Select(s => $"{s.ScoreText}: {(string.IsNullOrWhiteSpace(s.Number) ? "—" : s.Number)}")));
        html.Append(SectionHtml("МОМЕНТАЛЕН СКОР", currentScoreHtml));

        var scoreHtml = scoreHistory.Count == 0 ? EmptyHtml("Нема претходни скорови.")
            : RowHtml(string.Join("   |   ", scoreHistory.Select(s =>
                $"{s.ScoreText}: {(string.IsNullOrWhiteSpace(s.Number) ? "—" : s.Number)} ({s.RecordedAt.ToLocalTime():dd.MM.yyyy})")));
        html.Append(SectionHtml("ИСТОРИЈА НА СКОРОВИ", scoreHtml));

        var diagnosisHistoryHtml = diagnosisHistory.Count == 0 ? EmptyHtml("Нема дијагнози во последните 5 прегледи.")
            : string.Concat(diagnosisHistory.Select(d => RowHtml(
                $"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}",
                $"{(d.Encounter?.ScheduledStart ?? d.Encounter?.EncounterDate):dd.MM.yyyy}")));
        html.Append(SectionHtml("ИСТОРИЈА НА ДИЈАГНОЗИ", diagnosisHistoryHtml));

        var historyHtml = medicineHistory.Count == 0 ? EmptyHtml("Нема претходни лекови.")
            : string.Concat(medicineHistory.Select(m => RowHtml(m.Medicine?.FullName ?? m.Medicine?.Name ?? "Непознат лек", BuildMedicineDetails(m))));
        html.Append(SectionHtml("ИСТОРИЈА НА ЛЕКОВИ", historyHtml));
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

            // The report WebView is intentionally tiny/hidden in the preview page.
            // Activate the owning MAUI window before opening WebView2's native print UI
            // so the dialog is not left behind the app or treated as a separate app window.
            ActivateHostWindow();

            nativeWebView.CoreWebView2.ShowPrintUI(Microsoft.Web.WebView2.Core.CoreWebView2PrintDialogKind.Browser);

            // ShowPrintUI is modal; when it returns (Print or Cancel), return focus to EHMR.
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(150);
                ActivateHostWindow();
            });
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
        // Closing the preview should only navigate back inside Shell, never close the app window.
        try
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync("..");
                return;
            }

            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
                return;
            }

            await DisplayAlert("Преглед", "Не може да се врати на претходната страница.", "ОК");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Encounter preview back navigation failed: {ex}");
            await DisplayAlert("Преглед", "Не успеа враќањето на претходната страница.", "ОК");
        }
    }

#if WINDOWS
    private static void ActivateHostWindow()
    {
        try
        {
            var mauiWindow = Application.Current?.Windows.FirstOrDefault();
            if (mauiWindow?.Handler?.PlatformView is Microsoft.UI.Xaml.Window winUiWindow)
                winUiWindow.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Could not activate EHMR host window: {ex}");
        }
    }
#else
    private static void ActivateHostWindow() { }
#endif
}
