using System.Text.Json;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for reading and writing the config file for the CsvImporter tool.
/// </summary>
public static class ConfigFileUtils
{
    /// <summary>
    /// The path to the config directory.
    /// </summary>
    private static readonly string _configDirPath = Path.Combine(AppContext.BaseDirectory, ".config");

    /// <summary>
    /// The path to the config file.
    /// </summary>
    private static readonly string _configFilePath = Path.Combine(_configDirPath, "config.json");

    /// <summary>
    /// Gets the path to the config directory.
    /// </summary>
    /// <returns>The path to the config directory.</returns>
    public static string GetConfigDirPath() => _configDirPath;

    /// <summary>
    /// Gets the config for the CsvImporter tool.
    /// </summary>
    /// <returns>The config for the CsvImporter tool.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the config file.</exception>
    public static async Task<CsvImporterConfig> GetCsvImporterConfigAsync()
    {
        EnsureConfigDirExists();

        CsvImporterConfig csvImporterConfig;
        if (!File.Exists(_configFilePath))
        {
            csvImporterConfig = new();
            await SaveCsvImporterConfigAsync(csvImporterConfig);
        }
        else
        {
            string configJson = await File.ReadAllTextAsync(_configFilePath);
            csvImporterConfig = JsonSerializer.Deserialize(
                json: configJson,
                jsonTypeInfo: CoreJsonContext.Default.CsvImporterConfig
            ) ?? throw new Exception($"Unable to deserialize config file: {_configFilePath}");
        }

        return csvImporterConfig;
    }

    /// <summary>
    /// Saves the config for the CsvImporter tool.
    /// </summary>
    /// <param name="csvImporterConfig">The updated config for the CsvImporter tool.</param>
    public static async Task SaveCsvImporterConfigAsync(CsvImporterConfig csvImporterConfig)
    {
        EnsureConfigDirExists();

        string configJson = JsonSerializer.Serialize(
                value: csvImporterConfig,
                jsonTypeInfo: CoreJsonContext.Default.CsvImporterConfig
            );

        await File.WriteAllTextAsync(_configFilePath, configJson);
    }

    /// <summary>
    /// Checks if the config directory exists and creates it if it doesn't.
    /// </summary>
    private static void EnsureConfigDirExists()
    {
        if (!Directory.Exists(_configDirPath))
        {
            Directory.CreateDirectory(_configDirPath);
        }
    }
}