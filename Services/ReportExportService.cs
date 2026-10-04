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

                // Ширината на колоната во гридот (device units, пр. 180) се преведува
                // во Excel "character width" единица (приближно /7).
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
        var exportColumns = columns.Where(c => c.CellType!=SparkGridCellType.Actions && !IsSensitiveIdentityColumn(c)).ToList();
        var path = BuildOutputPath(reportTitle, "pdf");

        QuestPDF.Settings.License=LicenseType.Community;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);
                page.Size(PageSizes.A4.Landscape());
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Arial"));

                // ==========================================
                // HEADER - Медицински образец со Назив на установа
                // ==========================================
                page.Header().Column(col =>
                {
                    col.Item().Border(1).BorderColor("#000000").Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.RelativeColumn(1.1f); // Logo placeholder
                            cd.RelativeColumn(4.0f); // Institution + report title
                            cd.RelativeColumn(2.0f); // Issue date
                            cd.RelativeColumn(2.0f); // Period
                        });

                        // Future logo placeholder. The actual logo can be placed here
                        // later without changing the report header layout.
                        table.Cell().BorderRight(1).Padding(5).AlignCenter().AlignMiddle()
                            .Border(1).BorderColor("#9CA3AF")
                            .MinHeight(42)
                            .Text("LOGO")
                            .FontSize(8)
                            .FontColor("#6B7280");

                        // Keep the report title clean. Patient/basic data belongs
                        // in the report content below, not in the document title.
                        table.Cell().BorderRight(1).Padding(5).Column(c =>
                        {
                            c.Item().Text(institutionName.ToUpper())
                                   .Bold()
                                   .FontSize(10)
                                   .FontColor("#000000");

                            c.Item().PaddingTop(2).Text(reportTitle)
                                   .Bold()
                                   .FontSize(11)
                                   .FontColor("#1F2937");
                        });

                        // Клетка 2: ВИСТИНСКИ ЛОГИРАН КОРИСНИК
                        table.Cell().BorderRight(1).Padding(5).Column(c =>
                        {
                            c.Item().Text($"ДАТУМ НА ИЗДАВАЊЕ: {DateTime.Now:dd.MM.yyyy}").FontSize(7);
                    
                        
                        });

                        // Клетка 3: ОПСЕГ НА ПЕРИОД + ВКУПНА КОЛИЧИНА НА ЛЕКОТ
                        table.Cell().Padding(5).Column(c =>
                        {
                            c.Item().Text("ОПСЕГ НА ПЕРИОД:").Bold().FontSize(8);
                            c.Item().Text($"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}").FontSize(8);

                            if(IsMedicineConsumptionReport(reportTitle, selectedMedicine, selectedMedicineTotalQuantity))
                            {
                                c.Item().PaddingTop(3).Text("ВКУПНА КОЛИЧИНА НА ЛЕКОТ").Bold().FontSize(8);

                                if(!string.IsNullOrWhiteSpace(selectedMedicine) && selectedMedicineTotalQuantity.HasValue)
                                {
                                    c.Item().Text($"{selectedMedicine} : {selectedMedicineTotalQuantity.Value:0.##}").FontSize(8);
                                }
                                else
                                {
                                    foreach(var item in GetMedicineConsumptionSummary(rows))
                                        c.Item().Text($"{item.Medicine} : {item.Quantity}").FontSize(8);
                                }
                            }
                        });
                    });

                    // Забелешка за заштита на лични/медицински податоци
                    col.Item().BorderLeft(1).BorderRight(1).BorderBottom(1).Padding(3)
                       .Background("#F3F4F6")
                       .Text("НАПОМЕНА: Документот содржи заштитени здравствени податоци од Клиника за Реумаaтологија.")
                       .Italic().FontSize(7);

                    col.Item().Height(8);
                });

                // ==========================================
                // CONTENT - Содржина на Табелата
                // ==========================================
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        foreach(var col in exportColumns)
                            cols.RelativeColumn((float)GetColumnWidth(col));
                    });

                    table.Header(header =>
                    {
                        foreach(var col in exportColumns)
                        {
                            header.Cell().Border(1).BorderColor("#000000").Background("#E5E7EB").Padding(4)
                                .Text(col.Header).Bold();
                        }
                    });

                    foreach(var row in rows)
                    {
                        foreach(var col in exportColumns)
                        {
                            row.TryGetValue(col.Key, out var value);
                            table.Cell().Border(1).BorderColor("#D1D5DB").Padding(3)
                                .Text(CellToText(value));
                        }
                    }
                });

                // ==========================================
                // FOOTER - Потпис и Страница
                // ==========================================
                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(6).Row(row =>
                    {
                        row.RelativeItem().Text(x =>
                        {
                            x.Span("Страница ");
                            x.CurrentPageNumber();
                            x.Span(" од ");
                            x.TotalPages();
                        });

                        row.RelativeItem().AlignRight().Text($"Потпис на одговорно лице: ________________________________");
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
            // Fixed/Absolute ширина (пр. new GridLength(180)) се користи директно.
            // Auto/Star колони немаат конкретна вредност па добиваат разумен default.
            return column.Width.IsAbsolute&&column.Width.Value>0
                ? column.Width.Value
                : 180d;
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

        // Create folder if it doesn't exist
        if(!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, fileName);
    }
}

