using EHMR.Backups.Encryption;
using EHMR.Backups.Interfaces;
using EHMR.Backups.Services;
using EHMR.Infrastructure.Persistence.Configs;
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


        // Истиот избор на провајдер како при работа — инаку `dotnet ef` би
        // генерирал миграции за друга база од онаа што апликацијата ја користи.
        var databaseOptions =
            configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>()
            ??new DatabaseOptions();

        if(string.IsNullOrWhiteSpace(databaseOptions.SqlServerConnection))
        {
            databaseOptions.SqlServerConnection=
                configuration.GetConnectionString("Default")
                ??throw new InvalidOperationException(
                    "Нема ниту Database:SqlServerConnection ниту ConnectionStrings:Default.");
        }

        var builder = new DbContextOptionsBuilder<DesktopTherapyDbContext>();
        EHMRServiceCollectionExtensions.ConfigureDatabase(builder, databaseOptions);

        var options = builder.Options;


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