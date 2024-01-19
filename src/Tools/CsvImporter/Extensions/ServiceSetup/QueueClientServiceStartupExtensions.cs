using Azure.Core;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.QueueClient;

internal static class QueueClientServiceStartupExtensions
{
    public static IServiceCollection AddCsvImporterQueueClientService(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new(connectionString));

        return services;
    }

    public static IServiceCollection AddCsvImporterQueueClientService(this IServiceCollection services, string queueUri, TokenCredential tokenCredential)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new(new(queueUri), tokenCredential));

        return services;
    }
}
