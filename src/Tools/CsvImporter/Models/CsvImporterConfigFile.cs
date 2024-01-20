using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

public class CsvImporterConfigFile
{
    [JsonPropertyName("config")]
    public CsvImporterConfig Config { get; set; } = new();
}