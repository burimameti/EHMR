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

        public Task<string> ExportToPdfAsync(string reportTitle, IReadOnlyList<SparkGridColumn> columns, IReadOnlyList<SparkGridRow> rows)
        {
            var exportColumns = columns.Where(c => c.CellType!=SparkGridCellType.Actions).ToList();
            var path = BuildOutputPath(reportTitle, "pdf");

            QuestPDF.Settings.License=LicenseType.Community;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text(reportTitle).SemiBold().FontSize(16);

                    page.Content().Table(table =>
                    {
                        // Ширините на колоните од гридот (пр. GridLength(180)) се
                        // конвертираат во релативни пропорции за PDF табелата.
                        table.ColumnsDefinition(cols =>
                        {
                            foreach(var col in exportColumns)
                                cols.RelativeColumn((float)GetColumnWidth(col));
                        });

                        table.Header(header =>
                        {
                            foreach(var col in exportColumns)
                            {
                                header.Cell().Background("#5B6B79").Padding(4)
                                    .Text(col.Header).FontColor("#FFFFFF").SemiBold();
                            }
                        });

                        foreach(var row in rows)
                        {
                            foreach(var col in exportColumns)
                            {
                                row.TryGetValue(col.Key, out var value);
                                table.Cell().BorderBottom(1).BorderColor("#F0F2F5").Padding(4)
                                    .Text(CellToText(value));
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Страна ");
                        x.CurrentPageNumber();
                        x.Span(" од ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(path);

            return Task.FromResult(path);
        }

        // ================= HELPERS =================

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

