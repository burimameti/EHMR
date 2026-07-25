using System.IO.Compression;
using EHMR.Backups.Interfaces;

namespace EHMR.Backups.Services;

public sealed class CompressionService : ICompressionService
{
    public async Task<string> CompressAsync(
        string sourceFile,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(sourceFile))
            throw new ArgumentNullException(nameof(sourceFile));

        if(!File.Exists(sourceFile))
            throw new FileNotFoundException(sourceFile);

        var compressedFile = Path.ChangeExtension(sourceFile, ".zip");

        if(File.Exists(compressedFile))
            File.Delete(compressedFile);

        using(var archive = ZipFile.Open(
            compressedFile,
            ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(
                sourceFile,
                Path.GetFileName(sourceFile),
                CompressionLevel.Optimal);
        }

        await Task.CompletedTask;

        return compressedFile;
    }



    public async Task<string> DecompressAsync(
        string compressedFile,
        CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(compressedFile))
            throw new ArgumentNullException(nameof(compressedFile));

        if(!File.Exists(compressedFile))
            throw new FileNotFoundException(compressedFile);

        var outputFolder = Path.Combine(
            Path.GetDirectoryName(compressedFile)!,
            Path.GetFileNameWithoutExtension(compressedFile));

        if(Directory.Exists(outputFolder))
            Directory.Delete(outputFolder, true);

        Directory.CreateDirectory(outputFolder);

        ZipFile.ExtractToDirectory(
            compressedFile,
            outputFolder,
            overwriteFiles: true);

        var file = Directory
            .GetFiles(outputFolder)
            .FirstOrDefault();

        if(file==null)
            throw new InvalidOperationException(
                "Archive does not contain any files.");

        await Task.CompletedTask;

        return file;
    }



    public async Task<long> GetCompressedSizeAsync(
        string compressedFile,
        CancellationToken cancellationToken = default)
    {
        if(!File.Exists(compressedFile))
            return 0;

        await Task.CompletedTask;

        return new FileInfo(compressedFile).Length;
    }



    public async Task<bool> IsCompressedAsync(
        string file,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        return Path.GetExtension(file)
            .Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }
}