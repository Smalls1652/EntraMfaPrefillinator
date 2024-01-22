using EntraMfaPrefillinator.Tools.CsvImporter.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.ServiceSetup;

/// <summary>
/// Extension methods for setting up the <see cref="ICsvImporterSqliteService"/> service.
/// </summary>
public static class CsvImporterSqliteServiceStartupExtensions
{
    /// <summary>
    /// Adds the <see cref="ICsvImporterSqliteService"/> service to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to update.</param>
    /// <param name="options">Options for configuring the <see cref="ICsvImporterSqliteService"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddCsvImporterSqliteService(this IServiceCollection services, Action<CsvImporterSqliteServiceOptions> options)
    {
        services.Configure(options);
        services.TryAddSingleton<ICsvImporterSqliteService, CsvImporterSqliteService>();
        
        return services;
    }
}