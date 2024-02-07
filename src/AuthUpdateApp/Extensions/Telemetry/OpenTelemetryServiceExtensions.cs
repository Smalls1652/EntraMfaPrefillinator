using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry;

public static class OpenTelemetryServiceExtensions
{
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder) => AddOpenTelemetryLogging(builder, null);
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, string? azureAppInsightsConnectionString)
    {
        builder.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService("EntraMfaPrefillinator.AuthUpdateApp");

            logging
                .SetResourceBuilder(resourceBuilder)
                .AddOtlpExporter();

            if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
            {
                logging.AddAzureMonitorLogExporter(options =>
                {
                    options.ConnectionString = azureAppInsightsConnectionString;
                });
            }
        });

        return builder;
    }

    public static IServiceCollection AddOpenTelemetryMetricsAndTracing(this IServiceCollection services) => AddOpenTelemetryMetricsAndTracing(services, null);
    public static IServiceCollection AddOpenTelemetryMetricsAndTracing(this IServiceCollection services, string? azureAppInsightsConnectionString)
    {
        services.AddMetrics();

        services
            .AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddService("EntraMfaPrefillinator.AuthUpdateApp"))
            .WithMetrics(metrics =>
            {
                var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("EntraMfaPrefillinator.AuthUpdateApp");

                metrics.SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddOtlpExporter();

                if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = azureAppInsightsConnectionString;
                    });
                }
            })
            .WithTracing(tracing =>
            {
                var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("EntraMfaPrefillinator.AuthUpdateApp");

                tracing
                    .AddSource("EntraMfaPrefillinator.AuthUpdateApp.Endpoints")
                    .SetResourceBuilder(resourceBuilder)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();

                tracing.AddOtlpExporter();

                if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = azureAppInsightsConnectionString;
                    });
                }
            });

        return services;
    }
}