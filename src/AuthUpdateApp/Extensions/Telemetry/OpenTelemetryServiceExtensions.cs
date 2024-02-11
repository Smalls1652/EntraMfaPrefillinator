using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions.Telemetry;

/// <summary>
/// Extension methods for configuring OpenTelemetry in the
/// dependency injection container.
/// </summary>
internal static class OpenTelemetryServiceExtensions
{
    /// <summary>
    /// Configures OpenTelemetry logging.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
    /// <returns>The modified <see cref="ILoggingBuilder"/>.</returns>
    public static ILoggingBuilder AddOpenTelemetryLogging(this ILoggingBuilder builder) => AddOpenTelemetryLogging(builder, null);

    /// <summary>
    /// Configures OpenTelemetry logging with Azure Application Insights exporting.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/>.</param>
    /// <param name="azureAppInsightsConnectionString">The Azure Application Insights connection string.</param>
    /// <returns>The modified <see cref="ILoggingBuilder"/>.</returns>
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

    /// <summary>
    /// Configures OpenTelemetry metrics and tracing.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddOpenTelemetryMetricsAndTracing(this IServiceCollection services) => AddOpenTelemetryMetricsAndTracing(services, null);

    /// <summary>
    /// Configures OpenTelemetry metrics and tracing with Azure Application Insights exporting.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="azureAppInsightsConnectionString">The Azure Application Insights connection string.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
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
                    .AddSource("EntraMfaPrefillinator.AuthUpdateApp.Services.MainService")
                    .SetResourceBuilder(resourceBuilder)
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