using System.Text.Json.Serialization;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;

namespace EntraMfaPrefillinator.Tools.CsvImporter;

/// <summary>
/// Source generation context for classes used in the CsvImporter tool that
/// are serialized to and deserialized from JSON.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default
)]
[JsonSerializable(typeof(CsvImporterConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}