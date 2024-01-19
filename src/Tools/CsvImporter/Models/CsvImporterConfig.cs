using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

/// <summary>
/// Holds the configuration for the CsvImporter tool.
/// </summary>
public class CsvImporterConfig
{
    /// <summary>
    /// The path to the CSV file to import.
    /// </summary>
    [JsonPropertyName("csvFilePath")]
    public string CsvFilePath { get; set; } = string.Empty;

    /// <summary>
    /// The path to the last CSV file that was imported.
    /// </summary>
    [JsonPropertyName("lastCsvPath")]
    public string? LastCsvPath { get; set; }

    /// <summary>
    /// The last date and time the tool was run.
    /// </summary>
    [JsonPropertyName("lastRunDateTime")]
    public DateTimeOffset? LastRunDateTime { get; set; }

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
}