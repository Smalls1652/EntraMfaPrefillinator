using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

public sealed class CsvImporterConfigFile
{
    [JsonPropertyName("config")]
    public CsvImporterConfig Config { get; set; } = new();
}