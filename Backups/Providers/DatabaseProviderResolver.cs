// File: EHMR.Backups/Providers/DatabaseProviderResolver.cs
using EHMR.Backups.Interfaces;
using EHMR.Backups.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EHMR.Backups.Providers;

public sealed class DatabaseProviderResolver : IDatabaseProviderResolver
{
    private readonly IEnumerable<IDatabaseBackupProvider> _providers;
    private readonly IConfiguration _configuration;

    public DatabaseProviderResolver(
        IEnumerable<IDatabaseBackupProvider> providers,
        IConfiguration configuration)
    {
        _providers=providers;
        _configuration=configuration;
    }

    /// <summary>
    /// Resolves the provider that can handle the configured "Default" connection string.
    /// </summary>
    public IDatabaseBackupProvider Resolve()
    {
        var connectionString = _configuration.GetConnectionString("Default")
            ??throw new InvalidOperationException("Connection string 'Default' is not configured.");

        var provider = _providers.FirstOrDefault(p => p.CanHandle(connectionString));

        return provider
            ??throw new InvalidOperationException(
                "No database provider registered can handle the configured connection string.");
    }

    public IDatabaseBackupProvider Resolve(DatabaseProviderType type)
    {
        // Falls back to type-based selection when you have more than one provider registered
        // and don't want to rely purely on connection-string sniffing.
        return type switch
        {
            DatabaseProviderType.SqlServer => _providers.OfType<SqlServerBackupProvider>().FirstOrDefault()
                ??throw new InvalidOperationException("SqlServer provider not registered."),
            _ => throw new NotSupportedException($"Database provider type not supported: {type}")
        };
    }
}