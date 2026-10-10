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

        // Patient report: patient-level MKB-10 assignments only.
        // Encounter report: assignments belonging to that exact encounter.
        var diagnosesQuery=db.PatientMkb10Assignments
            .AsNoTracking()
            .Include(x => x.Mkb10Code)
            .Where(x => x.PatientId==patientId);

        diagnosesQuery=focusEncounter is not null
            ? diagnosesQuery.Where(x => x.EncounterId==focusEncounter.Id)
            : diagnosesQuery.Where(x => x.EncounterId==null);

        var diagnoses=await diagnosesQuery
            .OrderByDescending(x => x.DiagnosedAt)
            .Take(5)
            .ToListAsync();

        var medicines=await db.PatientMedicines
            .AsNoTracking()
            .Include(x => x.Medicine)
            .Include(x => x.ApplicationRegime)
            .Where(x => x.PatientId==patientId &&
                (focusEncounter != null
                    || x.EncounterId==focusEncounter.Id
                    || (!x.EncounterId.HasValue && x.IsActive)))
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync();

        var activeMedicines=medicines.Where(x => x.IsActive).ToList();
        var previousMedicines=medicines.Where(x => !x.IsActive).Take(5).ToList();

        var scores=await db.PatientScores
            .AsNoTracking()
            .Where(x => x.PatientId==patientId &&
                (focusEncounter !=null || x.EncounterId==focusEncounter.Id))
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
                                $"{score.ScoreText} - {value}",
                                $"{score.RecordedAt:dd.MM.yyyy}");
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
        column.Item().PaddingTop(7).Column(section =>
        {
            section.Item()
                .BorderBottom(1)
                .BorderColor("#111827")
                .PaddingBottom(3)
                .Text(title)
                .Bold()
                .FontSize(9)
                .FontColor("#111827");

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
