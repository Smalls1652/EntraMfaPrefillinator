using Azure.Core;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.Telemetry;

internal static class OpenTelemetryServiceExtensions
{
    /// <summary>
    /// Configures OpenTelemetry logging.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The modified <see cref="ILoggingBuilder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, string instanceId) => AddOpenTelemetryLogging(builder, instanceId, null, null);

    /// <summary>
    /// Configures OpenTelemetry logging with Azure Application Insights exporting.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
    /// <param name="azureAppInsightsConnectionString">The Azure Application Insights connection string.</param>
    /// <param name="tokenCredential">The Azure token credential.</param>
    /// <returns>The modified <see cref="ILoggingBuilder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder, string instanceId, string? azureAppInsightsConnectionString, TokenCredential? tokenCredential)
    {

        Dictionary<string, object> openTelemetryProperties = new()
        {
            { "service.name", "Tools.CsvImporter" },
            { "service.namespace", "EntraMfaPrefillinator" },
            { "service.instance.id", instanceId }
        };

        builder.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService("EntraMfaPrefillinator.Tools.CsvImporter")
                .AddAttributes(openTelemetryProperties);

            logging
                .SetResourceBuilder(resourceBuilder)
                .AddOtlpExporter();

            if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
            {
                logging.AddAzureMonitorLogExporter(options =>
                {
                    options.ConnectionString = azureAppInsightsConnectionString;

                    if (tokenCredential is not null)
                    {
                        options.Credential = tokenCredential;
                    }
                });
            }
        });

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry metrics and tracing.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddOpenTelemetryMetricsAndTracing(this IServiceCollection services, string instanceId) => AddOpenTelemetryMetricsAndTracing(services, instanceId, null, null);

    /// <summary>
    /// Configures OpenTelemetry metrics and tracing with Azure Application Insights exporting.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="azureAppInsightsConnectionString">The Azure Application Insights connection string.</param>
    /// <param name="tokenCredential">The Azure token credential.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddOpenTelemetryMetricsAndTracing(this IServiceCollection services, string instanceId, string? azureAppInsightsConnectionString, TokenCredential? tokenCredential)
    {
        services.AddMetrics();

        Dictionary<string, object> openTelemetryProperties = new()
        {
            { "service.name", "Tools.CsvImporter" },
            { "service.namespace", "EntraMfaPrefillinator" },
            { "service.instance.id", instanceId }
        };

        services
            .AddOpenTelemetry()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddService("EntraMfaPrefillinator.Tools.CsvImporter"))
            .WithMetrics(metrics =>
            {
                var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("EntraMfaPrefillinator.Tools.CsvImporter")
                    .AddAttributes(openTelemetryProperties);

                metrics.SetResourceBuilder(resourceBuilder)
                    .AddRuntimeInstrumentation()
                    .AddHttpClientInstrumentation();

                metrics.AddOtlpExporter();

                if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
                {
                    metrics.AddAzureMonitorMetricExporter(options =>
                    {
                        options.ConnectionString = azureAppInsightsConnectionString;

                        if (tokenCredential is not null)
                        {
                            options.Credential = tokenCredential;
                        }
                    });
                }

            })
            .WithTracing(tracing =>
            {
                var resourceBuilder = ResourceBuilder
                    .CreateDefault()
                    .AddService("EntraMfaPrefillinator.Tools.CsvImporter")
                    .AddAttributes(openTelemetryProperties);

                tracing
                    .AddSource("EntraMfaPrefillinator.Tools.CsvImporter.MainService")
                    .SetResourceBuilder(resourceBuilder)
                    .AddHttpClientInstrumentation();

                tracing.AddOtlpExporter();

                if (azureAppInsightsConnectionString is not null && !string.IsNullOrEmpty(azureAppInsightsConnectionString))
                {
                    tracing.AddAzureMonitorTraceExporter(options =>
                    {
                        options.ConnectionString = azureAppInsightsConnectionString;

                        if (tokenCredential is not null)
                        {
                            options.Credential = tokenCredential;
                        }
                    });
                }
            });

        return services;
    }
}
