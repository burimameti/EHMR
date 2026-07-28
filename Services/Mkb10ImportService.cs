using ClosedXML.Excel;
using EHMR.Domain.Entities;
using EHMR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace EHMR.Infrastructure.Services;

/// <summary>
/// Result of an МКБ-10 catalog import run. Returned so the calling UI can show the user
/// exactly what happened instead of a generic "done" message.
/// </summary>
public record Mkb10ImportResult(int Inserted, int Updated, int Skipped, List<string> Errors);

/// <summary>
/// Progress snapshot reported during an import run, for binding to a ProgressBar.
/// </summary>
public record Mkb10ImportProgress(int Processed, int Total);

/// <summary>
/// Reads an official МКБ-10 (ICD-10) export and upserts it into the Mkb10Codes lookup table.
/// Expects a UTF-8 CSV/TSV with a header row containing at least "Code" and "Description"
/// columns (case-insensitive), and optionally "Chapter". Delimiter is auto-detected between
/// ';' and ',' by inspecting the header row, since МКБ-10 exports from different sources use
/// either convention. No external CSV library is used — the codebase doesn't currently
/// reference one, so a small quote-aware manual parser is used instead to avoid adding a
/// new dependency for a single import screen.
/// </summary>
public class Mkb10ImportService
{
    private readonly IDbContextFactory<DesktopTherapyDbContext> _dbFactory;

    // Report progress at most this often, so a 14k-row catalog doesn't flood the UI thread
    // with a dispatch per row.
    private const int ProgressReportInterval = 25;

    public Mkb10ImportService(IDbContextFactory<DesktopTherapyDbContext> dbFactory)
    {
        _dbFactory=dbFactory;
    }

    public async Task<Mkb10ImportResult> ImportAsync(
    string filePath,
    IProgress<Mkb10ImportProgress>? progress = null,
    CancellationToken cancellationToken = default)
    {
        if(!File.Exists(filePath))
        {
            return new Mkb10ImportResult(
                0,
                0,
                0,
                new() { $"Датотеката не постои: {filePath}" });
        }

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".xlsx" => await ImportFromExcelAsync(filePath, progress, cancellationToken),
            ".xls" => await ImportFromExcelAsync(filePath, progress, cancellationToken),

            _ => new Mkb10ImportResult(
                    0,
                    0,
                    0,
                    new() { $"Неподдржан тип на датотека: {extension}" })
        };
    }

    private async Task<Mkb10ImportResult> ImportFromExcelAsync(
        string filePath,
        IProgress<Mkb10ImportProgress>? progress,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        int inserted = 0, updated = 0, skipped = 0;

        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Set<Mkb10Code>()
            .ToDictionaryAsync(
                x => x.Code,
                x => x,
                StringComparer.OrdinalIgnoreCase,
                cancellationToken);

        using var workbook = new XLWorkbook(filePath);

        var worksheet = workbook.Worksheet(1);

        var rows = worksheet.RowsUsed().ToList();

        if(rows.Count<=1)
        {
            errors.Add("Excel датотеката е празна.");

            return new Mkb10ImportResult(0, 0, 0, errors);
        }

        int totalRows = rows.Count-1;
        string? previousCode = null;
        string? previousDescription = null;
        string? previousChapter = null;
        for(int i = 2; i<=rows.Count; i++)
        {
            var row = worksheet.Row(i);

            string rowExtract = row.Cell(1).GetString().Trim().ToUpperInvariant();

            var parts = rowExtract.Split('|', 2);
            if(parts.Length<2)
            {
                skipped++;
                continue;
            }

            // Редот изгледа „A00.0 | ОПИС" — по делењето шифрата задржува празно
            // место пред исправката, па се запишуваше како "A00.0 ". Секое
            // барање по точна шифра потоа не наоѓаше ништо.
            string code = parts[0].Trim();
            string description = parts[1].Trim();
            string chapter = row.Cell(3).GetString().Trim();

            // Skip consecutive duplicates
            if(code==previousCode&&
                description==previousDescription
                )
            {
                skipped++;
                continue;
            }

            previousCode=code;
            previousDescription=description;
            previousChapter=chapter;

            if(string.IsNullOrWhiteSpace(code))
            {
                skipped++;
                continue;
            }

            if(existing.TryGetValue(code, out var current))
            {
                if(current.Description!=description||
                    current.Chapter!=chapter)
                {
                    current.Description=description;
                    current.Chapter=chapter;
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                db.Set<Mkb10Code>().Add(new Mkb10Code
                {
                    Id=Guid.NewGuid(),
                    Code=code,
                    Description=description,
                    Chapter=chapter,
                    IsActive=true
                });

                inserted++;
            }

            ReportProgress(progress, i-1, totalRows);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new Mkb10ImportResult(inserted, updated, skipped, errors);
    }

    private static void ReportProgress(IProgress<Mkb10ImportProgress>? progress, int rowIndex, int total)
    {
        if(progress is null) return;
        if(rowIndex%ProgressReportInterval==0||rowIndex==total)
        {
            progress.Report(new Mkb10ImportProgress(rowIndex, total));
        }
    }

    /// <summary>
    /// Minimal quote-aware CSV/TSV line splitter: handles fields wrapped in double quotes
    /// (including an escaped "" inside a quoted field) so descriptions containing the
    /// delimiter itself don't break the column count. Not a full RFC 4180 parser, but
    /// sufficient for the flat single-line records МКБ-10 exports use.
    /// </summary>
    private static List<string> SplitLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for(int i = 0; i<line.Length; i++)
        {
            char c = line[i];

            if(inQuotes)
            {
                if(c=='"')
                {
                    if(i+1<line.Length&&line[i+1]=='"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes=false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if(c=='"')
                {
                    inQuotes=true;
                }
                else if(c==delimiter)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}