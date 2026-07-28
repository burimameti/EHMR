using EHMR.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace EHMR.Infrastructure.Persistence.Configs;

/// <summary>
/// Го полни каталогот на МКБ-10 шифри од <c>MKB10.xlsx</c> во папката на апликацијата.
///
/// Мора да оди пред сите други seeder-и: дијагнозите и прегледите се врзуваат за
/// шифри, па ако каталогот е празен тие остануваат без важечка шифра.
/// Order = 0 — најнискиот; останатите почнуваат од 1.
///
/// Се извршува само кога табелата е празна. Повторно подигање не го дира каталогот,
/// ниту рачно додадените шифри.
/// </summary>
public sealed class Mkb10CatalogSeeder : IEntitySeeder
{
    private const string CatalogFileName = "MKB10.xlsx";

    private readonly Mkb10ImportService _importService;

    public Mkb10CatalogSeeder(Mkb10ImportService importService)
    {
        _importService=importService;
    }

    public int Order => 0;

    public async Task SeedAsync(DesktopTherapyDbContext context, CancellationToken ct = default)
    {
        if(await context.Mkb10Codes.AnyAsync(ct))
            return;

        var path = ResolveCatalogPath();

        if(path is null)
        {
            // Недостигот на каталог не смее да го запре подигањето — апликацијата
            // работи и без шифри, само со намалена функционалност.
            Console.WriteLine($"[MKB10] {CatalogFileName} не е пронајден — каталогот останува празен.");
            return;
        }

        Console.WriteLine($"[MKB10] Се вчитува каталог од {path}");

        var result = await _importService.ImportAsync(path, progress: null, cancellationToken: ct);

        Console.WriteLine(
            $"[MKB10] Внесени {result.Inserted}, ажурирани {result.Updated}, прескокнати {result.Skipped}.");

        foreach(var error in result.Errors.Take(5))
            Console.WriteLine($"[MKB10] {error}");
    }

    /// <summary>
    /// Бара во излезната папка, па во коренот на проектот — при развој
    /// апликацијата се пушта од bin, а датотеката стои во коренот.
    /// </summary>
    private static string? ResolveCatalogPath()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, CatalogFileName),
            Path.Combine(Directory.GetCurrentDirectory(), CatalogFileName)
        };

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for(var i = 0; i<6&&dir is not null; i++)
        {
            candidates.Add(Path.Combine(dir.FullName, CatalogFileName));
            dir=dir.Parent;
        }

        return candidates.FirstOrDefault(File.Exists);
    }
}
