using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

/// <summary>
/// Holds the configuration for the CsvImporter tool.
/// </summary>
public sealed class CsvImporterConfig
{
    /// <summary>
    /// The path to the CSV file to import.
    /// </summary>
    [JsonPropertyName("csvFilePath")]
    public string CsvFilePath { get; set; } = string.Empty;

    /// <summary>
    /// The last date and time the tool was run.
    /// </summary>
    [JsonPropertyName("lastRunDateTime")]
    public DateTimeOffset? LastRunDateTime { get; set; } = DateTimeOffset.MinValue.UtcDateTime;

    /// <summary>
    /// Whether the tool should run in dry run mode.
    /// </summary>
    [JsonPropertyName("dryRunEnabled")]
    public bool DryRunEnabled { get; set; }

    /// <summary>
    /// The Azure Storage account queue URI.
    /// </summary>
    [JsonPropertyName("queueUri")]
    public string? QueueUri { get; set; }

    [JsonPropertyName("queueMessageTTL")]
    public QueueMessageTimeToLiveConfig QueueMessageTTL { get; set; } = new();
}