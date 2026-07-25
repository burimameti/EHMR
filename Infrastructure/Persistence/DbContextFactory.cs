using EHMR.Backups.Encryption;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.IO.Packaging;

namespace EHMR.Infrastructure.Persistence;

public sealed class DesktopTherapyDbContextFactory
    : IDesignTimeDbContextFactory<DesktopTherapyDbContext>
{
    public DesktopTherapyDbContext CreateDbContext(string[] args)
    {
        // Load configuration
        IConfiguration configuration =
            new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();


        // SQL Connection
        var connectionString =
            configuration.GetConnectionString("Default")
            ??throw new InvalidOperationException(
                "Connection string 'Default' was not found.");


        // EF Options
        var options =
            new DbContextOptionsBuilder<DesktopTherapyDbContext>()
                .UseSqlServer(connectionString)
                .Options;


        // Encryption
        var encryptionOptions =
            Options.Create(
                configuration
                    .GetSection(EncryptionOptions.SectionName)
                    .Get<EncryptionOptions>()
                ??throw new InvalidOperationException(
                    "Encryption configuration is missing."));

        IEncryptionService encryptionService =
            new EncryptionService(encryptionOptions);


        // Create DbContext
        return new DesktopTherapyDbContext(
            options,
            exceptionParser: null,
            encryptionService: encryptionService);
    }
}