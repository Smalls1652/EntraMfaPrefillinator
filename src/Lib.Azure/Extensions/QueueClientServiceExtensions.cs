using Azure.Core;

using EntraMfaPrefillinator.Lib.Azure.Services;

using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Lib.Azure.Extensions;

public static class QueueClientServiceExtensions
{
    public static IServiceCollection AddQueueClientService(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new(connectionString));

        return services;
    }

    public static IServiceCollection AddQueueClientService(this IServiceCollection services, string queueUri, TokenCredential tokenCredential)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new(new(queueUri), tokenCredential));

        return services;
    }

    public static IServiceCollection AddQueueClientService(this IServiceCollection services)
    {
        services.AddSingleton<IQueueClientService, QueueClientService>(service => new("DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;"));

        return services;
    }
}
