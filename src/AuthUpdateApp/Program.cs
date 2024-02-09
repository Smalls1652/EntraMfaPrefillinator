using EntraMfaPrefillinator.AuthUpdateApp.Extensions;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry;
using EntraMfaPrefillinator.AuthUpdateApp.Hosting;
using EntraMfaPrefillinator.AuthUpdateApp.Services;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using CancellationTokenSource cts = new();

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .RemoveAll<IHostLifetime>();

builder.Services
    .AddSingleton<IHostLifetime, AuthUpdateAppHostLifetime>();

builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile(
        path: builder.Environment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json",
        optional: true,
        reloadOnChange: true
    );

builder.Logging
    .AddConsole()
    .AddOpenTelemetryLogging(
        azureAppInsightsConnectionString: builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTIONSTRING")
    );

builder.Services
    .AddOpenTelemetryMetricsAndTracing(
        azureAppInsightsConnectionString: builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTIONSTRING")
    );

builder.Services
    .AddSingleton<IQueueClientService, QueueClientService>(
        service => new(
            connectionString: builder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTIONSTRING") ?? throw new NullReferenceException("AZURE_STORAGE_CONNECTIONSTRING is not set")
        )
    );

builder.Services
    .AddGraphClientService(
        graphClientConfig: new()
        {
            ClientId = builder.Configuration.GetValue<string>("CLIENT_ID") ?? throw new NullReferenceException("CLIENT_ID is not set"),
            TenantId = builder.Configuration.GetValue<string>("TENANT_ID") ?? throw new NullReferenceException("TENANT_ID is not set"),
            Credential = new GraphClientCredential(
                credentialType: GraphClientCredentialType.ClientSecret,
                clientSecret: builder.Configuration.GetValue<string>("CLIENT_SECRET") ?? throw new NullReferenceException("CLIENT_SECRET is not set")
            ),
            ApiScopes = ["https://graph.microsoft.com/.default"]
        },
        disableAuthUpdate: builder.Configuration.GetValue<bool>("ENABLE_DRY_RUN")
    );

builder.Services
    .AddMainService(options =>
    {
        options.MaxMessages = builder.Configuration.GetValue("MAX_MESSAGES", 25);
    });

using var app = builder.Build();

await app.RunAsync(cts.Token);
