using EHMR.Domain.Interfaces;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EHMR.Services;

public sealed class PatientClinicalReportService : IPatientClinicalReportService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _factory;

    public PatientClinicalReportService(IDbContextFactory<DesktopTherapyDbContext> factory)
    {
        _factory=factory;
    }


    public async Task<string> GenerateHtmlAsync(
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

        var nationalId = patient.NationalId;
        var maskedId = string.IsNullOrWhiteSpace(nationalId) ? "—" :
            nationalId.Length <= 4 ? new string('•', nationalId.Length) : new string('•', nationalId.Length - 4) + nationalId[^4..];
        var doctorName = encounter?.Doctor?.FullName ?? patient.Doctor?.FullName;
        var fields = new System.Text.StringBuilder("<div class='grid'>");
        fields.Append(Field("Пациент", patient.FullName)).Append(Field("СЗБО", patient.SzboNumber))
            .Append(Field("ЕМБГ", maskedId)).Append(Field("Датум на раѓање", patient.BirthDate.ToString("dd.MM.yyyy")))
            .Append(Field("Пол", patient.Gender.ToString())).Append(Field("Телефон", patient.Phone))
            .Append(Field("Адреса", JoinAddress(patient.Address, patient.City, patient.PostalCode)))
            .Append(Field("Реуматолог", doctorName));
        if (encounter != null) fields.Append(Field("Датум на преглед", encounter.EncounterDate.ToString("dd.MM.yyyy HH:mm")));
        fields.Append("</div>");
        html.Append(SectionHtml("ОСНОВНИ ИНФОРМАЦИИ", fields.ToString()));

        if (encounter != null)
        {
            var details = new System.Text.StringBuilder("<div class='grid'>");
            details.Append(Field("Број на преглед", encounter.EncounterNumber))
                .Append(Field("Причина за посета", encounter.ReasonForVisit ?? encounter.Appointment?.ReasonForVisit))
                .Append(Field("Главна тегоба", encounter.ChiefComplaint))
                .Append(Field("Анамнеза", encounter.HistoryOfPresentIllness))
                .Append(Field("Проценка", encounter.Assessment)).Append(Field("План", encounter.Plan))
                .Append(Field("Забелешка", encounter.Notes ?? encounter.ClinicalNotes ?? encounter.Appointment?.ClinicalNotes))
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
                var nextFields = "<div class='grid'>" + Field("Датум и време", next.ScheduledStart.ToString("dd.MM.yyyy HH:mm")) +
                    Field("Лекар", next.Doctor?.FullName) + Field("Причина", next.ReasonForVisit) +
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

    public async Task<string> GeneratePdfAsync(
        Guid patientId,
        Guid? encounterId = null,
        Guid? appointmentId = null,
        string? title = null,
        string? generatedBy = null)
    {
        await using var db=await _factory.CreateDbContextAsync();

        var patient=await db.Patients
            .AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id==patientId)
            ??throw new InvalidOperationException("Пациентот не е пронајден.");

        var focusEncounter=encounterId.HasValue
            ? await db.Encounters
                .AsNoTracking()
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .Include(x => x.Appointment)
                .FirstOrDefaultAsync(x => x.Id==encounterId.Value && x.PatientId==patientId)
            : null;

        if(encounterId.HasValue && focusEncounter is null)
            throw new InvalidOperationException("Прегледот не е пронајден.");

        // Current encounter data and bounded patient history are loaded separately.
        var diagnoses = focusEncounter is null
            ? await db.PatientMkb10Assignments.AsNoTracking().Include(x => x.Mkb10Code)
                .Where(x => x.PatientId==patientId && x.EncounterId==null)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Mkb10Code!.Code).ToListAsync()
            : await db.PatientMkb10Assignments.AsNoTracking().Include(x => x.Mkb10Code)
                .Where(x => x.PatientId==patientId && x.EncounterId==focusEncounter.Id)
                .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Mkb10Code!.Code).ToListAsync();

        var allPatientMedicines=await db.PatientMedicines.AsNoTracking()
            .Include(x => x.Medicine).Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync();

        var encounterMedicines = focusEncounter is null
            ? allPatientMedicines.Where(x => x.EncounterId==null && x.IsActive).Take(5).ToList()
            : allPatientMedicines.Where(x => x.EncounterId==focusEncounter.Id).Take(5).ToList();

        var encounterPrescriptions = focusEncounter is null
            ? new List<Domain.Entities.Prescription>()
            : await db.Prescriptions.AsNoTracking()
                .Where(x => x.PatientId==patientId && x.EncounterId==focusEncounter.Id)
                .OrderBy(x => x.Medication).ToListAsync();

        var medicineHistory=allPatientMedicines
            .Where(x => focusEncounter is null || x.EncounterId!=focusEncounter.Id)
            .Take(5).ToList();

        var scores=await db.PatientScores.AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.RecordedAt).Take(5).ToListAsync();

        var previousEncounters = focusEncounter is null
            ? new List<Domain.Entities.Encounter>()
            : await db.Encounters.AsNoTracking().Include(x => x.Doctor).ThenInclude(x => x.User)
                .Where(x => x.PatientId==patientId && x.Id!=focusEncounter.Id &&
                    (x.ScheduledStart ?? x.EncounterDate) < (focusEncounter.ScheduledStart ?? focusEncounter.EncounterDate))
                .OrderByDescending(x => x.ScheduledStart ?? x.EncounterDate).Take(5).ToListAsync();

        var nextAppointment = focusEncounter is null
            ? null
            : await db.Appointments.AsNoTracking().Include(x => x.Doctor).ThenInclude(x => x.User)
                .Where(x => x.PatientId==patientId &&
                    x.Status==Domain.Entities.AppointmentStatus.Scheduled &&
                    x.ScheduledStart > (focusEncounter.ScheduledStart ?? focusEncounter.EncounterDate))
                .OrderBy(x => x.ScheduledStart).FirstOrDefaultAsync();

        var outputTitle=focusEncounter is not null
            ? $"Извештај за преглед бр. {focusEncounter.EncounterNumber}"
            : (string.IsNullOrWhiteSpace(title) ? "Извештај за пациент" : title.Trim());

        var path=BuildOutputPath(outputTitle);

        QuestPDF.Settings.License=LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(30);
                page.MarginVertical(26);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                page.Header().Element(c => BuildHeader(
                    c,
                    outputTitle,
                    focusEncounter));

                page.Content().Column(column =>
                {
                    Section(column, "ОСНОВНИ ИНФОРМАЦИИ", () =>
                    {
                        TwoColumn(column, "Пациент", patient.FullName);
                        TwoColumn(column, "ЕЗБО", patient.SzboNumber);
                        TwoColumn(column, "ЕМБГ", MaskNationalId(patient.NationalId));
                        TwoColumn(column, "Датум на раѓање", patient.BirthDate.ToString("dd.MM.yyyy"));
                        TwoColumn(column, "Пол", patient.Gender.ToString());
                        TwoColumn(column, "Телефон", patient.Phone);
                        TwoColumn(column, "Адреса", JoinAddress(patient.Address, patient.City, patient.PostalCode));
                        TwoColumn(column, "Реуматолог", focusEncounter?.Doctor?.FullName ?? patient.Doctor?.FullName);
                        if(focusEncounter is not null)
                            TwoColumn(column, "Датум на преглед", focusEncounter.EncounterDate.ToString("dd.MM.yyyy HH:mm"));
                    });

                    if(focusEncounter is not null)
                    {
                        Section(column, "ПОДАТОЦИ ЗА ПРЕГЛЕДОТ", () =>
                        {
                            TwoColumn(column, "Број на преглед", focusEncounter.EncounterNumber);
                            TwoColumn(column, "Причина за посета", focusEncounter.ReasonForVisit ?? focusEncounter.Appointment?.ReasonForVisit);
                            TwoColumn(column, "Главна тегоба", focusEncounter.ChiefComplaint);
                            TwoColumn(column, "Анамнеза", focusEncounter.HistoryOfPresentIllness);
                            TwoColumn(column, "Проценка", focusEncounter.Assessment);
                            TwoColumn(column, "План", focusEncounter.Plan);
                            TwoColumn(column, "Забелешка", focusEncounter.Notes ?? focusEncounter.ClinicalNotes ?? focusEncounter.Appointment?.ClinicalNotes);
                        });

                        Section(column, "ДИЈАГНОЗА ЗА ОВОЈ ПРЕГЛЕД (МКБ-10)", () =>
                        {
                            if(diagnoses.Count==0) { Empty(column, "Нема внесена МКБ-10 дијагноза за овој преглед."); return; }
                            foreach(var d in diagnoses)
                                Row(column, $"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}",
                                    d.IsPrimary ? "Примарна дијагноза" : null);
                        });

                        Section(column, "ЛЕКОВИ ПОВРЗАНИ СО ОВОЈ ПРЕГЛЕД", () =>
                        {
                            if(encounterMedicines.Count==0 && encounterPrescriptions.Count==0)
                            { Empty(column, "Нема лекови поврзани со овој преглед."); return; }
                            foreach(var medicine in encounterMedicines)
                                Row(column, medicine.Medicine?.FullName ?? medicine.Medicine?.Name ?? "Непознат лек",
                                    BuildMedicineDetails(medicine));
                            foreach(var prescription in encounterPrescriptions)
                                Row(column, prescription.Medication ?? "Рецепт",
                                    string.Join(" · ", new[] { prescription.Dosage, prescription.Instructions, prescription.Status }
                                        .Where(x => !string.IsNullOrWhiteSpace(x))));
                        });

                        if(nextAppointment is not null)
                            Section(column, "ПОДАТОЦИ ЗА СЛЕДНА КОНТРОЛА", () =>
                            {
                                TwoColumn(column, "Датум и време", nextAppointment.ScheduledStart.ToString("dd.MM.yyyy HH:mm"));
                                TwoColumn(column, "Лекар", nextAppointment.Doctor?.FullName);
                                TwoColumn(column, "Причина", nextAppointment.ReasonForVisit);
                                TwoColumn(column, "Статус", nextAppointment.Status.ToString());
                            });

                        Section(column, "ИСТОРИЈА НА ПРЕТХОДНИ ПРЕГЛЕДИ (ДО 5)", () =>
                        {
                            if(previousEncounters.Count==0) { Empty(column, "Нема претходни прегледи."); return; }
                            foreach(var previous in previousEncounters)
                                Row(column, $"Преглед бр. {previous.EncounterNumber}",
                                    $"{(previous.ScheduledStart ?? previous.EncounterDate):dd.MM.yyyy HH:mm} · {previous.Doctor?.FullName ?? "—"} · {previous.Status}");
                        });
                    }

                    Section(column, "ИСТОРИЈА НА ПОСЛЕДНИ 5 СКОРОВИ", () =>
                    {
                        if(scores.Count==0) { Empty(column, "Нема внесени скорови."); return; }
                        foreach(var score in scores)
                        {
                            column.Item().PaddingVertical(3).BorderBottom(1).BorderColor("#E2E8F0").Row(row =>
                            {
                                row.RelativeItem().Text($"{score.ScoreText} — {(string.IsNullOrWhiteSpace(score.Number) ? "—" : score.Number)}")
                                    .Bold().FontSize(9.5f);
                                row.ConstantItem(82).AlignRight().Text(score.RecordedAt.ToLocalTime().ToString("dd.MM.yyyy"))
                                    .FontSize(9);
                            });
                        }
                    });

                    Section(column, "ИСТОРИЈА НА ЛЕКОВИ (ДО 5)", () =>
                    {
                        if(medicineHistory.Count==0) { Empty(column, "Нема претходни лекови."); return; }
                        foreach(var medicine in medicineHistory)
                            Row(column, medicine.Medicine?.FullName ?? medicine.Medicine?.Name ?? "Непознат лек",
                                BuildMedicineDetails(medicine));
                    });
                });

                page.Footer().Row(row =>
                {
                    row.RelativeItem().Text("EHMR").FontSize(7).FontColor("#64748B");
                    row.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Страница ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        }).GeneratePdf(path);

        return path;
    }

    private static void BuildHeader(
        QuestPDF.Infrastructure.IContainer container,
        string title,
        Domain.Entities.Encounter? focusEncounter)
    {
        container
            .PaddingBottom(7)
            .BorderBottom(1)
            .BorderColor("#111827")
            .Row(row =>
            {
                row.ConstantItem(62)
                    .AlignLeft()
                    .AlignMiddle()
                    .Text("EHMR")
                    .Bold()
                    .FontSize(14)
                    .FontColor("#64748B");

                row.RelativeItem()
                    .AlignRight()
                    .AlignMiddle()
                    .Text(title)
                    .Bold()
                    .FontSize(11)
                    .FontColor("#111827");
            });
    }

    private static void Section(
        ColumnDescriptor column,
        string title,
        Action content)
    {
        column.Item().PaddingTop(10).Column(section =>
        {
            section.Item()
                .BorderBottom(1)
                .BorderColor("#111827")
                .PaddingBottom(3)
                .Text(title)
                .Bold()
                .FontSize(10)
                .FontColor("#111827");

            content();
        });
    }

    private static void TwoColumn(ColumnDescriptor column, string label, string? value)
    {
        column.Item().PaddingVertical(2).Row(row =>
        {
            row.ConstantItem(145).Text(label).Bold().FontSize(9.5f);
            row.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(9.5f);
        });
    }

    private static void Row(ColumnDescriptor column, string title, string? value)
    {
        column.Item().PaddingVertical(3).BorderBottom(1).BorderColor("#E2E8F0").Column(c =>
        {
            c.Item().Text(title).Bold().FontSize(9.5f);
            if(!string.IsNullOrWhiteSpace(value))
                c.Item().PaddingTop(2).Text(value).FontSize(9);
        });
    }

    private static void TextLine(ColumnDescriptor column, string label, string? value)
    {
        if(string.IsNullOrWhiteSpace(value)) return;
        column.Item().PaddingVertical(2).Row(row =>
        {
            row.ConstantItem(105).Text(label).Bold().FontSize(8);
            row.RelativeItem().Text(value).FontSize(8);
        });
    }

    private static void Empty(ColumnDescriptor column, string text)
    {
        column.Item().Padding(4).Text(text).FontSize(8).FontColor("#64748B");
    }

    private static string BuildMedicineDetails(Domain.Entities.PatientMedicine medicine)
    {
        var parts=new List<string>();

        if(!string.IsNullOrWhiteSpace(medicine.Dosage))
            parts.Add($"Доза: {medicine.Dosage}");

        if(medicine.Quantity>0)
            parts.Add($"Количина: {FormatDecimal(medicine.Quantity)}");

        if(!string.IsNullOrWhiteSpace(medicine.ApplicationRegime?.Regime))
            parts.Add($"Режим: {medicine.ApplicationRegime.Regime}");

        return string.Join(" · ", parts);
    }

    private static string MaskNationalId(string? nationalId)
    {
        if(string.IsNullOrWhiteSpace(nationalId))
            return "—";

        var value=new string(nationalId.Where(char.IsLetterOrDigit).ToArray());
        if(value.Length<=4)
            return new string('•', value.Length);

        return new string('•', value.Length-4)+value[^4..];
    }

    private static string JoinAddress(string? address, string? city, string? postal)
        => string.Join(", ", new[] { address, city, postal }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static string FormatDecimal(decimal value)
        => value.ToString("0.################", System.Globalization.CultureInfo.InvariantCulture);

    private static string FormatBytes(long value)
        => value<=0 ? "—" : value switch
        {
            < 1024 => $"{value} B",
            < 1024*1024 => $"{value / 1024d:0.##} KB",
            _ => $"{value / 1024d / 1024d:0.##} MB"
        };

    private static string BuildOutputPath(string title)
    {
        var safe=string.Concat(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        if(string.IsNullOrWhiteSpace(safe)) safe="PatientReport";
        var folder=@"C:\GenerateReports";
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, $"{safe}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }
}
