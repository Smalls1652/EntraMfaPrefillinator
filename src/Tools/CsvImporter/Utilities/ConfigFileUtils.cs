using System.Text.Json;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Models.Exceptions;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for reading and writing the config file for the CsvImporter tool.
/// </summary>
internal static class ConfigFileUtils
{
    public static void EnsureRequiredOptionsAreSet(CsvImporterConfig config)
    {
        if (config.CsvFilePath is null || string.IsNullOrEmpty(config.CsvFilePath))
        {
            throw new ConfigPropertyException("The 'csvFilePath' option must be set in the config file.", "csvFilePath");
        }

        if (config.QueueUri is null || string.IsNullOrEmpty(config.QueueUri))
        {
            throw new ConfigPropertyException("The 'queueUri' option must be set in the config file.", "queueUri");
        }
    }

    public static async Task SaveConfigAsync(CsvImporterConfigFile configFile, string configFilePath)
    {

        string configJson = JsonSerializer.Serialize(
                value: configFile,
                jsonTypeInfo: ConfigJsonContext.Default.CsvImporterConfigFile
            );

        await File.WriteAllTextAsync(configFilePath, configJson);
    }
}