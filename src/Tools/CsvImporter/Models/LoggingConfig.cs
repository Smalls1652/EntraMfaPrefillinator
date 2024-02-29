using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter;

public sealed class LoggingConfig
{
    [JsonPropertyName("enableFileLogging")]
    public bool EnableFileLogging { get; set; } = true;

    [JsonPropertyName("enableOpenTelemetry")]
    public bool EnableOpenTelemetry { get; set; } = false;

    [JsonPropertyName("enableOpenTelelmetryAzureAppInsights")]
    public bool EnableOpenTelelmetryAzureAppInsights { get; set; } = false;

    [JsonPropertyName("azureAppInsightsConnectionString")]
    public string? AzureAppInsightsConnectionString { get; set; }

    [JsonPropertyName("enableOpenTelemetryAzureTokenCredential")]
    public bool EnableOpenTelemetryAzureTokenCredential { get; set; } = false;

    [JsonPropertyName("openTelemetryInstanceId")]
    public string OpenTelemetryInstanceId { get; set; } = Guid.NewGuid().ToString();
}
