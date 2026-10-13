// EHMR.Services/ReportExportService.cs
using ClosedXML.Excel;
using EHMR.Domain.Interfaces;
using EHMR.Resources.Controls;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace EHMR.Services;

    public class ReportExportService : IReportExportService
    {
        public async Task<string> ExportToExcelAsync(string reportTitle, IReadOnlyList<SparkGridColumn> columns, IReadOnlyList<SparkGridRow> rows)
        {
            // Actions колоната (копчиња "Преглед"/"Промени") нема смисла во извоз
            var exportColumns = columns.Where(c => c.CellType!=SparkGridCellType.Actions && !IsSensitiveIdentityColumn(c)).ToList();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add(SafeSheetName(reportTitle));

            // Хедер
            for(int c = 0; c<exportColumns.Count; c++)
            {
                var cell = sheet.Cell(1, c+1);
                cell.Value=exportColumns[c].Header;
                cell.Style.Font.Bold=true;
                cell.Style.Fill.BackgroundColor=XLColor.FromHtml("#5B6B79");
                cell.Style.Font.FontColor=XLColor.White;

                var widthUnits = GetColumnWidth(exportColumns[c]);
                sheet.Column(c+1).Width=Math.Max(10, widthUnits/7.0);
            }

            // Редови
            for(int r = 0; r<rows.Count; r++)
            {
                for(int c = 0; c<exportColumns.Count; c++)
                {
                    rows[r].TryGetValue(exportColumns[c].Key, out var value);
                    sheet.Cell(r+2, c+1).Value=CellToText(value);
                }
            }

            sheet.SheetView.FreezeRows(1);
            sheet.RangeUsed()?.SetAutoFilter();

            var path = BuildOutputPath(reportTitle, "xlsx");
            using(var stream = File.Create(path))
                workbook.SaveAs(stream);

            return await Task.FromResult(path);
        }

    public Task<string> ExportToPdfAsync(
        string reportTitle,
        string institutionName,
        string generatedBy,
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<SparkGridColumn> columns,
        IReadOnlyList<SparkGridRow> rows,
        string? selectedMedicine = null,
        decimal? selectedMedicineTotalQuantity = null)
    {
        var exportColumns = columns
            .Where(c => c.CellType != SparkGridCellType.Actions &&
                        !string.Equals(c.Key, "GroupExpand", StringComparison.OrdinalIgnoreCase) &&
                        !IsSensitiveIdentityColumn(c))
            .ToList();
        var path = BuildOutputPath(reportTitle, "pdf");

        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(24);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                page.Header().Column(header =>
                {
                    header.Item().AlignCenter().Text(institutionName)
                        .Bold().FontSize(13).FontColor("#000000");

                    header.Item().PaddingTop(4).AlignCenter().Text(reportTitle)
                        .Bold().FontSize(10).FontColor("#000000");

                    header.Item().PaddingTop(5).BorderBottom(1).BorderColor("#000000")
                        .PaddingBottom(5).Row(row =>
                        {
                            row.RelativeItem().Text(reportTitle.Contains("сите периоди", StringComparison.OrdinalIgnoreCase)
                                ? "ПЕРИОД: СИТЕ ПЕРИОДИ"
                                : $"ПЕРИОД: {startDate:dd.MM.yyyy} – {endDate:dd.MM.yyyy}")
                                .FontSize(8);
                            row.ConstantItem(150).AlignRight()
                                .Text($"ДАТУМ НА ИЗВЕШТАЈ: {DateTime.Now:dd.MM.yyyy}")
                                .FontSize(8);
                        });
                });

                page.Content().PaddingTop(8).Column(contentColumn =>
                {
                    contentColumn.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            foreach(var column in exportColumns)
                                cols.RelativeColumn((float)GetColumnWidth(column));
                        });

                        table.Header(header =>
                        {
                            foreach(var column in exportColumns)
                            {
                                header.Cell()
                                    .Border(0.7f).BorderColor("#000000")
                                    .Background("#E5E7EB")
                                    .PaddingVertical(5).PaddingHorizontal(4)
                                    .Text(column.Header).Bold().FontSize(7);
                            }
                        });

                        foreach(var row in rows)
                        {
                            foreach(var column in exportColumns)
                            {
                                row.TryGetValue(column.Key, out var value);
                                table.Cell()
                                    .Border(0.5f).BorderColor("#555555")
                                    .PaddingVertical(4).PaddingHorizontal(3)
                                    .Text(CellToText(value));
                            }
                        }
                    });

                    if(IsMedicineConsumptionReport(reportTitle, selectedMedicine, selectedMedicineTotalQuantity))
                    {
                        contentColumn.Item().PaddingTop(8).AlignRight().Column(summary =>
                        {
                            if(!string.IsNullOrWhiteSpace(selectedMedicine) && selectedMedicineTotalQuantity.HasValue)
                            {
                                summary.Item().Text($"Лек: {selectedMedicine}").Bold().FontSize(9);
                                summary.Item().Text($"Вкупна количина: {selectedMedicineTotalQuantity.Value:0.##}")
                                    .Bold().FontSize(9);
                            }
                            else
                            {
                                foreach(var item in GetMedicineConsumptionSummary(rows))
                                    summary.Item().Text($"Вкупна количина — {item.Medicine}: {item.Quantity}")
                                        .FontSize(8);
                            }
                        });
                    }
                });

                page.Footer().PaddingTop(8).Column(footer =>
                {
                    footer.Item().BorderTop(0.7f).BorderColor("#000000").PaddingTop(6).Row(row =>
{
    row.RelativeItem().Text($"Датум: {DateTime.Now:dd.MM.yyyy}").FontSize(8);
    row.RelativeItem().Text("Извештајот го подготвил (име и презиме): __________________________").FontSize(7);
});
                    footer.Item().PaddingTop(4).AlignRight().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(7));
                        text.Span("Страница ");
                        text.CurrentPageNumber();
                        text.Span(" од ");
                        text.TotalPages();
                    });
                });
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }

    // ================= HELPERS =================

    private static bool IsSensitiveIdentityColumn(SparkGridColumn column)
    {
        var header = column.Header?.Trim() ?? string.Empty;
        var key = column.Key?.Trim() ?? string.Empty;

        return key.Equals("NationalId", StringComparison.OrdinalIgnoreCase) ||
               header.Contains("ЕМБГ", StringComparison.OrdinalIgnoreCase) ||
               header.Contains("EMBG", StringComparison.OrdinalIgnoreCase) ||
               header.Contains("Матичен број", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMedicineConsumptionReport(
        string reportTitle,
        string? selectedMedicine,
        decimal? selectedMedicineTotalQuantity) =>
        (selectedMedicine!=null && selectedMedicineTotalQuantity.HasValue) ||
        reportTitle.Contains("Потрошувачка по лек", StringComparison.OrdinalIgnoreCase) ||
        reportTitle.Contains("Пациенти со лек", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<(string Medicine, string Quantity)> GetMedicineConsumptionSummary(
        IReadOnlyList<SparkGridRow> rows)
    {
        return rows
            .Select(row =>
            {
                row.TryGetValue("MedicineConsumptionMedicine", out var medicineValue);
                row.TryGetValue("MedicineConsumptionQuantity", out var quantityValue);
                return new
                {
                    Medicine = medicineValue?.ToString()?.Trim() ?? string.Empty,
                    Quantity = decimal.TryParse(quantityValue?.ToString(), out var q) ? q : 0m
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Medicine))
            .GroupBy(x => x.Medicine, StringComparer.CurrentCultureIgnoreCase)
            .Select(g => (g.First().Medicine, g.Sum(x => x.Quantity).ToString("0.##")))
            .ToList();
    }

    private static double GetColumnWidth(SparkGridColumn column)
    {
        if(column.Width.IsAbsolute && column.Width.Value > 0)
            return column.Width.Value;

        // Preserve the relative proportions configured for the grid columns.
        return column.Width.IsStar && column.Width.Value > 0
            ? column.Width.Value
            : 1d;
    }

    private static string CellToText(object? value) => value switch
    {
        null => string.Empty,
        SparkBadgeValue badge => badge.Text,
        DateTime dt => dt.ToString("dd.MM.yyyy"),
        _ => value.ToString()??string.Empty
    };

    private static string SafeSheetName(string title)
    {
        var invalid = new[] { '\\', '/', '*', '[', ']', ':', '?' };
        var clean = new string(title.Where(c => !invalid.Contains(c)).ToArray());
        return clean.Length>31 ? clean[..31] : (clean.Length==0 ? "Извештај" : clean);
    }

    private static string BuildOutputPath(string title, string extension)
    {
        var safeName = string.Concat(title.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        var fileName = $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}";

        const string folder = @"C:\GenerateReports";

        if(!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, fileName);
    }
}

