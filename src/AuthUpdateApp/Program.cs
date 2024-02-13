using EntraMfaPrefillinator.AuthUpdateApp;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions;
using EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry;
using EntraMfaPrefillinator.AuthUpdateApp.Hosting;
using EntraMfaPrefillinator.Lib.Models.Graph;
using EntraMfaPrefillinator.Lib.Services;
using EntraMfaPrefillinator.Lib.Services.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using CancellationTokenSource cts = new();

using ILoggerFactory programLoggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole()
        .SetMinimumLevel(LogLevel.Information);
});

ILogger programLogger = programLoggerFactory.CreateLogger("EntraMfaPrefillinator.AuthUpdateApp");

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
    .AddSimpleConsole()
    .AddOpenTelemetryLogging(
        azureAppInsightsConnectionString: builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTIONSTRING")
    );

builder.Services
    .AddOpenTelemetryMetricsAndTracing(
        azureAppInsightsConnectionString: builder.Configuration.GetValue<string>("APPINSIGHTS_CONNECTIONSTRING")
    );

builder.Services
    .AddQueueClientService(
        connectionString: builder.Configuration.GetValue<string>("AZURE_STORAGE_CONNECTIONSTRING") ?? throw new ConfigException("AZURE_STORAGE_CONNECTIONSTRING is not set.")
    )
    .AddGraphClientService(options =>
    {
        options.ClientId = builder.Configuration.GetValue<string>("CLIENT_ID") ?? throw new ConfigException("CLIENT_ID is not set.");
        options.TenantId = builder.Configuration.GetValue<string>("TENANT_ID") ?? throw new ConfigException("TENANT_ID is not set.");
        options.Credential = new GraphClientCredential(
            credentialType: GraphClientCredentialType.ClientSecret,
            clientSecret: builder.Configuration.GetValue<string>("CLIENT_SECRET") ?? throw new ConfigException("CLIENT_SECRET is not set.")
        );
        options.ApiScopes = ["https://graph.microsoft.com/.default"];

        options.DisableUpdateMethods = builder.Configuration.GetValue<bool>("ENABLE_DRY_RUN");
    });

builder.Services
    .AddMainService(options =>
    {
        options.MaxMessages = builder.Configuration.GetValue("MAX_MESSAGES", 32);
    });

using var app = builder.Build();

try
{
    await app.RunAsync(cts.Token);
}
catch (ConfigException ex)
{
    programLogger.LogError("A required config value is missing: {Message}", ex.Message);
    cts.Cancel();
}
catch (Exception ex)
{
    programLogger.LogError(ex, "An unhandled exception occurred.");
    cts.Cancel();
}