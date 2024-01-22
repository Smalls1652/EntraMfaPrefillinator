using EntraMfaPrefillinator.Tools.CsvImporter.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.ServiceSetup;

/// <summary>
/// Extension methods for setting up the <see cref="ICsvFileReaderService"/>.
/// </summary>
internal static class CsvFileReaderServiceStartupExtensions
{
    /// <summary>
    /// Adds the <see cref="ICsvFileReaderService"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddCsvFileReaderService(this IServiceCollection services)
    {
        services.AddSingleton<ICsvFileReaderService, CsvFileReaderService>();

        return services;
    }
}