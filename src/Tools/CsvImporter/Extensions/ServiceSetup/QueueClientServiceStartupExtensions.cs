using Azure.Core;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.QueueClient;

internal static class QueueClientServiceStartupExtensions
{
    public static IServiceCollection AddCsvImporterQueueClientService(this IServiceCollection services, string queueUri, TokenCredential tokenCredential)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new(new(queueUri), tokenCredential));

        return services;
    }

    public static IServiceCollection AddCsvImporterQueueClientService(this IServiceCollection services)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new("DefaultEndpointsProtocol=https;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=https://127.0.0.1:10001/devstoreaccount1;"));

        return services;
    }
}
