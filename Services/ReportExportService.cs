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
            var exportColumns = columns.Where(c => c.CellType!=SparkGridCellType.Actions).ToList();

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
    IReadOnlyList<SparkGridRow> rows)
    {
        var exportColumns = columns.Where(c => c.CellType!=SparkGridCellType.Actions).ToList();
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
                            cd.RelativeColumn(4); // Институција и Динамички Наслов
                            cd.RelativeColumn(2); // Логиран Корисник и Време
                            cd.RelativeColumn(2); // Период
                        });

                        // Клетка 1: УСТАНОВА + ДИНАМИЧКИ НАСЛОВ (Месечен/Периодичен...)
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
                            c.Item().Text($"ИЗРАБОТИЛ: ");
                        
                        });

                        // Клетка 3: ОПСЕГ НА ПЕРИОД + ВКУПНА КОЛИЧИНА НА ЛЕКОТ
                        table.Cell().Padding(5).Column(c =>
                        {
                            c.Item().Text("ОПСЕГ НА ПЕРИОД:").Bold().FontSize(8);
                            c.Item().Text($"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}").FontSize(8);

                            if(IsMedicineConsumptionReport(reportTitle))
                            {
                                c.Item().PaddingTop(3).Text("ВКУПНА КОЛИЧИНА:").Bold().FontSize(8);

                                foreach(var item in GetMedicineConsumptionSummary(rows))
                                    c.Item().Text($"{item.Medicine} : {item.Quantity}").FontSize(8);
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

                        row.RelativeItem().AlignRight().Text($"Изработил: ________________________________");
                    });
                });
            });
        }).GeneratePdf(path);

        return Task.FromResult(path);
    }
    // ================= HELPERS =================

    private static bool IsMedicineConsumptionReport(string reportTitle) =>
        reportTitle.Contains("Потрошувачка по лек", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<(string Medicine, string Quantity)> GetMedicineConsumptionSummary(
        IReadOnlyList<SparkGridRow> rows)
    {
        var result = new List<(string Medicine, string Quantity)>();

        foreach(var row in rows)
        {
            row.TryGetValue("MedicineConsumptionMedicine", out var medicineValue);
            row.TryGetValue("MedicineConsumptionQuantity", out var quantityValue);

            var medicine = medicineValue?.ToString()?.Trim();
            var quantity = quantityValue?.ToString()?.Trim();

            if(string.IsNullOrWhiteSpace(medicine) || string.IsNullOrWhiteSpace(quantity))
                continue;

            result.Add((medicine, quantity));
        }

        return result;
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

