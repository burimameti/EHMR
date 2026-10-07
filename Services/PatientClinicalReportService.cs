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

        var diagnoses=await db.Diagnoses
            .AsNoTracking()
            .Include(x => x.Mkb10Code)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.DiagnosedAt)
            .ToListAsync();

        var medicines=await db.PatientMedicines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.IsActive)
            .ToListAsync();

        var scores=await db.PatientScores
            .AsNoTracking()
            .Include(x => x.Encounter)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.RecordedAt)
            .ToListAsync();

        var appointments=await db.Appointments
            .AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.TherapyCycle)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.ScheduledStart)
            .ToListAsync();

        var encounters=await db.Encounters
            .AsNoTracking()
            .Include(x => x.Doctor).ThenInclude(x => x.User)
            .Include(x => x.TherapyCycle)
            .Include(x => x.Diagnoses).ThenInclude(x => x.Mkb10Code)
            .Include(x => x.PatientScore)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.EncounterDate)
            .ToListAsync();

        var therapies=await db.TherapyCycles
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();

        var prescriptions=await db.Prescriptions
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.IssuedDate)
            .ToListAsync();

        var documents=await db.PatientDocuments
            .AsNoTracking()
            .Where(x => x.PatientId==patientId && !x.IsDeleted)
            .OrderByDescending(x => x.UploadedAt)
            .ToListAsync();

        var outputTitle=string.IsNullOrWhiteSpace(title)
            ? "Детален извештај за пациент"
            : title.Trim();
        var path=BuildOutputPath(outputTitle);

        QuestPDF.Settings.License=LicenseType.Community;

        var focusEncounter=encounterId.HasValue
            ? encounters.FirstOrDefault(x => x.Id==encounterId.Value)
            : null;
        var focusAppointment=appointmentId.HasValue
            ? appointments.FirstOrDefault(x => x.Id==appointmentId.Value)
            : null;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9));

                page.Header().Element(c => BuildHeader(
                    c, outputTitle, patient, focusEncounter, focusAppointment, generatedBy));

                page.Content().Column(column =>
                {
                    Section(column, "ЛИЧНИ И ОСНОВНИ ПОДАТОЦИ", () =>
                    {
                        TwoColumn(column, "Пациент", patient.FullName);
                        //TwoColumn(column, "Пациентски број", patient.PatientNumber);              
                        TwoColumn(column, "ЕЗБО", patient.SzboNumber);
                        TwoColumn(column, "Датум на раѓање", patient.BirthDate.ToString("dd.MM.yyyy"));
                        TwoColumn(column, "Пол", patient.Gender.ToString());
                        TwoColumn(column, "Телефон", patient.Phone);
                        TwoColumn(column, "Е-пошта", patient.Email);
                        TwoColumn(column, "Адреса", JoinAddress(patient.Address, patient.City, patient.PostalCode));
                       // TwoColumn(column, "Матичен реуматолог", patient.Doctor?.FullName ?? "—");
                        TwoColumn(column, "Статус", patient.Status.ToString());
                        //if(patient.Status==Domain.Entities.PatientStatus.Inactive)
                        //    TwoColumn(column, "Причина за неактивност", patient.InactiveReason);
                        TwoColumn(column, "Регистрација", patient.RegistrationDate.ToString("dd.MM.yyyy"));
                    });

                    //Section(column, "МЕДИЦИНСКИ И БИОЛОШКИ ПОДАТОЦИ", () =>
                    //{
                    //   // TwoColumn(column, "Крвна група", patient.BloodType);
                    //    TwoColumn(column, "Итна контакт личност", patient.EmergencyContactName);
                    //    TwoColumn(column, "Телефон за итен контакт", patient.EmergencyContactPhone);
                    //    TwoColumn(column, "Однос", patient.EmergencyRelationship);
                    //});

                    Section(column, "МКБ-10 ДИЈАГНОЗИ", () =>
                    {
                        if(diagnoses.Count==0) Empty(column, "Нема внесени дијагнози.");
                        foreach(var d in diagnoses)
                            Row(column,
                                $"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}",
                                $"{d.DiagnosedAt:dd.MM.yyyy} · {(d.IsPrimary ? "Примарна" : "Дополнителна")}");
                    });

                    Section(column, "АКТИВНА ТЕРАПИЈА", () =>
                    {
                        var active=medicines.Where(x => x.IsActive).ToList();
                        if(active.Count==0) Empty(column, "Нема активна терапија.");
                        foreach(var m in active)
                            Row(column,
                                m.Medicine?.Name ?? "Непознат лек",
                                $"Доза: {m.Dosage} · Количина: {FormatDecimal(m.Quantity)} · Режим: {m.ApplicationRegime?.Regime ?? "—"}");
                    });

                    Section(column, "ПРЕТХОДНИ ЛЕКОВИ / ТЕРАПИИ", () =>
                    {
                        var previous=medicines.Where(x => !x.IsActive).ToList();
                        if(previous.Count==0) Empty(column, "Нема претходно неактивни лекови.");
                        foreach(var m in previous)
                            Row(column,
                                m.Medicine?.Name ?? "Непознат лек",
                                $"Доза: {m.Dosage} · Количина: {FormatDecimal(m.Quantity)} · Режим: {m.ApplicationRegime?.Regime ?? "—"} · До: {(m.EndDate.HasValue ? m.EndDate.Value.ToString("dd.MM.yyyy") : "—")}");
                    });

                    Section(column, "ПРЕТХОДНИ ТЕРАПЕВТСКИ ЦИКЛУСИ", () =>
                    {
                        if(therapies.Count==0) Empty(column, "Нема внесени терапевтски циклуси.");
                        foreach(var t in therapies)
                            Row(column,
                                $"{t.TherapyCyleNumber} · {t.Status?.ToString() ?? "—"}",
                                $"{t.StartDate:dd.MM.yyyy} – {(t.EndDate.HasValue ? t.EndDate.Value.ToString("dd.MM.yyyy") : "—")} · {t.DecisionText ?? t.Notes ?? "Без забелешка"}");
                    });

                    //Section(column, "СКОРОВИ", () =>
                    //{
                    //    //Posleden skor
                    //    if(scores.Count==0) Empty(column, "Нема внесени скорови.");
                    //   // foreach(var s in scores.FirstOrDefault())
                    //        Row(scores.FirstOrDefault().ScoreText,
                    //            scores.FirstOrDefault().ScoreText, null);
                    //});

                    //Section(column, "ПРЕГЛЕДИ", () =>
                    //{
                    //    if(encounters.Count==0) Empty(column, "Нема внесени прегледи.");
                    //    foreach(var e in encounters)
                    //    {
                    //        var diagnosisText=string.Join(", ",
                    //            e.Diagnoses.Select(d => $"{d.Mkb10Code?.Code} {d.Mkb10Code?.Description}"));
                    //        Row(column,
                    //            $"{e.EncounterNumber} · {e.EncounterDate:dd.MM.yyyy HH:mm} · {e.Status}",
                    //            $"Реуматолог: {e.Doctor?.FullName ?? "—"} · Скор: {e.PatientScore?.ScoreText ?? "—"}");
                    //        //if(!string.IsNullOrWhiteSpace(e.ChiefComplaint))
                    //        //    TextLine(column, "Главна поплака", e.ChiefComplaint);
                    //        if(!string.IsNullOrWhiteSpace(e.ReasonForVisit))
                    //            TextLine(column, "Причина за посета", e.ReasonForVisit);
                    //        if(!string.IsNullOrWhiteSpace(e.HistoryOfPresentIllness))
                    //            TextLine(column, "Анамнеза", e.HistoryOfPresentIllness);
                    //        if(!string.IsNullOrWhiteSpace(e.Assessment))
                    //            TextLine(column, "Проценка", e.Assessment);
                    //        if(!string.IsNullOrWhiteSpace(e.Plan))
                    //            TextLine(column, "План", e.Plan);
                    //        if(!string.IsNullOrWhiteSpace(e.ClinicalNotes ?? e.Notes))
                    //            TextLine(column, "Забелешка", e.ClinicalNotes ?? e.Notes);
                    //        if(!string.IsNullOrWhiteSpace(diagnosisText))
                    //            TextLine(column, "Дијагнози", diagnosisText);
                    //    }
                    //});

                    //Section(column, "ТЕРМИНИ", () =>
                    //{
                    //    if(appointments.Count==0) Empty(column, "Нема внесени термини.");
                    //    foreach(var a in appointments)
                    //        Row(column,
                    //            $"{a.AppointmentNumber} · {a.ScheduledStart:dd.MM.yyyy HH:mm}–{a.ScheduledEnd:HH:mm} · {a.Status}",
                    //            $"Реуматолог: {a.Doctor?.FullName ?? "—"} · Причина: {a.ReasonForVisit}");
                    //        // ClinicalNotes is rendered below as its own field to preserve long notes.
                    //    foreach(var a in appointments.Where(x => !string.IsNullOrWhiteSpace(x.ClinicalNotes)))
                    //        TextLine(column, "Забелешка за термин", $"{a.ScheduledStart:dd.MM.yyyy HH:mm}: {a.ClinicalNotes}");
                    //});

                    //Section(column, "РЕЦЕПТИ", () =>
                    //{
                    //    if(prescriptions.Count==0) Empty(column, "Нема внесени рецепти.");
                    //    foreach(var p in prescriptions)
                    //        Row(column,
                    //            $"{p.PrescriptionNumber} · {p.Medication ?? "—"}",
                    //            $"Доза: {p.Dosage ?? "—"} · Статус: {p.Status} · Издадено: {p.IssuedDate:dd.MM.yyyy} · Важи до: {p.ExpiryDate:dd.MM.yyyy}");
                    //        foreach(var p in prescriptions.Where(x => !string.IsNullOrWhiteSpace(x.Instructions) || !string.IsNullOrWhiteSpace(x.Notes)))
                    //            TextLine(column, "Инструкции / забелешка", $"{p.Instructions} {p.Notes}");
                    //});

                    //Section(column, "ДОКУМЕНТИ", () =>
                    //{
                    //    if(documents.Count==0) Empty(column, "Нема прикачени документи.");
                    //    foreach(var d in documents)
                    //        Row(column,
                    //            $"{d.Title} · {d.DocumentType}",
                    //            $"{d.FileName} · {d.ContentType} · {d.UploadedAt:dd.MM.yyyy HH:mm} · {FormatBytes(d.FileSize)}");
                    //});
                });

                page.Footer().Row(row =>
                {
                    //row.RelativeItem().Text("EHMR · Доверлив медицински извештај");
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
        Domain.Entities.Patient patient,
        Domain.Entities.Encounter? focusEncounter,
        Domain.Entities.Appointment? focusAppointment,
        string? generatedBy)
    {
        container
            .Border(1)
            .BorderColor("#CBD5E1")
            .Padding(10)
            .Column(c =>
            {
                c.Item().Text(title).Bold().FontSize(16).FontColor("#000000");
               // c.Item().PaddingTop(2).Text(patient.FullName).Bold().FontSize(12);
                //c.Item().PaddingTop(2).Text(
                //    $"ЕЗБО: {patient.SzboNumber} · ЕМБГ: {patient.NationalId} · " +
                //    $"Реуматолог: {patient.Doctor?.FullName ?? "—"}").FontSize(9);
                c.Item().PaddingTop(2).Text(
                    $"Датум: {DateTime.Now:dd.MM.yyyy HH:mm} · " );
                   
                //if(focusEncounter is not null)
                //    c.Item().PaddingTop(4).Text(
                //        $"Фокусиран преглед: {focusEncounter.EncounterNumber} · {focusEncounter.EncounterDate:dd.MM.yyyy HH:mm}")
                //        .Bold().FontSize(9);
                //if(focusAppointment is not null)
                //    c.Item().PaddingTop(2).Text(
                //        $"Фокусиран термин: {focusAppointment.AppointmentNumber} · {focusAppointment.ScheduledStart:dd.MM.yyyy HH:mm}")
                //        .Bold().FontSize(9);
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
                .Background("#E6FFFB")
                .BorderBottom(2)
                .BorderColor("#AAAAAA")
                .Padding(6)
                .Text(title)
                .Bold()
                .FontSize(10)
                .FontColor("#0F172A");
            content();
        });
    }

    private static void TwoColumn(ColumnDescriptor column, string label, string? value)
    {
        column.Item().PaddingVertical(2).Row(row =>
        {
            row.ConstantItem(145).Text(label).Bold().FontSize(8.5f);
            row.RelativeItem().Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(8.5f);
        });
    }

    private static void Row(ColumnDescriptor column, string title, string? value)
    {
        column.Item().PaddingVertical(3).BorderBottom(1).BorderColor("#E2E8F0").Column(c =>
        {
            c.Item().Text(title).Bold().FontSize(8.5f);
            if(!string.IsNullOrWhiteSpace(value))
                c.Item().PaddingTop(1).Text(value).FontSize(8);
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
