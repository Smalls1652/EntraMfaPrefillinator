using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

IHostBuilder hostBuilder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults();

hostBuilder
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    });

hostBuilder
    .ConfigureServices(services =>
    {
        services.AddSingleton<IGraphClientService, GraphClientService>(
            provider => new GraphClientService(
                graphClientConfig: new()
                {
                    ClientId = provider.GetRequiredService<IConfiguration>()["clientId"],
                    TenantId = provider.GetRequiredService<IConfiguration>()["tenantId"],
                    Credential = new GraphClientCredential(
                        credentialType: GraphClientCredentialType.ClientSecret,
                        clientSecret: provider.GetRequiredService<IConfiguration>()["clientSecret"]
                    ),
                    ApiScopes = new[]
                    {
                        "https://graph.microsoft.com/.default"
                    }
                }
            )
        );

        services.AddSingleton<IQueueClientService, QueueClientService>(
                provider => new(
                    connectionString: provider.GetRequiredService<IConfiguration>()["storageConnectionString"]
                )
            );
    });

var host = hostBuilder.Build();

await host.RunAsync();