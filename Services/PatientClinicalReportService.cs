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

        var focusEncounter=encounterId.HasValue
            ? await db.Encounters
                .AsNoTracking()
                .Include(x => x.Doctor).ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id==encounterId.Value && x.PatientId==patientId)
            : null;

        if(encounterId.HasValue && focusEncounter is null)
            throw new InvalidOperationException("Прегледот не е пронајден.");

        var diagnoses=await db.Diagnoses
            .AsNoTracking()
            .Include(x => x.Mkb10Code)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.DiagnosedAt)
            .Take(5)
            .ToListAsync();

        var medicines=await db.PatientMedicines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var activeMedicines=medicines.Where(x => x.IsActive).ToList();
        var previousMedicines=medicines.Where(x => !x.IsActive).Take(5).ToList();

        var scores=await db.PatientScores
            .AsNoTracking()
            .Where(x => x.PatientId==patientId)
            .OrderByDescending(x => x.RecordedAt)
            .Take(5)
            .ToListAsync();

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
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f));

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

                    Section(column, "АКТИВНИ ЛЕКОВИ", () =>
                    {
                        if(activeMedicines.Count==0)
                        {
                            Empty(column, "Нема активни лекови.");
                            return;
                        }

                        foreach(var m in activeMedicines)
                        {
                            Row(
                                column,
                                m.Medicine?.Name ?? "Непознат лек",
                                BuildMedicineDetails(m));
                        }
                    });

                    Section(column, "НЕАКТИВНИ ЛЕКОВИ", () =>
                    {
                        if(previousMedicines.Count==0)
                        {
                            Empty(column, "Нема неактивни лекови.");
                            return;
                        }

                        foreach(var m in previousMedicines)
                        {
                            Row(
                                column,
                                m.Medicine?.Name ?? "Непознат лек",
                                BuildMedicineDetails(m));
                        }
                    });

                    Section(column, "ДИЈАГНОЗИ", () =>
                    {
                        if(diagnoses.Count==0)
                        {
                            Empty(column, "Нема внесени дијагнози.");
                            return;
                        }

                        foreach(var d in diagnoses)
                        {
                            Row(
                                column,
                                $"{d.Mkb10Code?.Code ?? "—"} — {d.Mkb10Code?.Description ?? "Без опис"}",
                                $"{d.DiagnosedAt:dd.MM.yyyy}");
                        }
                    });

                    Section(column, "СКОРОВИ", () =>
                    {
                        if(scores.Count==0)
                        {
                            Empty(column, "Нема внесени скорови.");
                            return;
                        }

                        foreach(var score in scores)
                        {
                            var value=string.IsNullOrWhiteSpace(score.Number) ? "—" : score.Number;
                            Row(
                                column,
                                score.ScoreText,
                                $"{value} · {score.RecordedAt:dd.MM.yyyy}");
                        }
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
