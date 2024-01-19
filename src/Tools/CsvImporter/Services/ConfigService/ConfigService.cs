using System.Text.Json;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter;

public class ConfigService : IConfigService
{
    private readonly ILogger<ConfigService> _logger;
    private readonly ConfigServiceOptions _configServiceOptions;

    public ConfigService(ILogger<ConfigService> logger, IOptions<ConfigServiceOptions> options)
    {
        _logger = logger is not null ? logger : NullLogger<ConfigService>.Instance;
        _configServiceOptions = options.Value;
    }

    public CsvImporterConfig? Config { get; set; }

    public string GetConfigDirPath() => _configServiceOptions.ConfigDirPath;

    public async Task LoadConfigAsync()
    {
        _logger.LogInformation("Loading config...");

        _logger.LogInformation("Ensuring config directory exists at '{configDirPath}'...",  _configServiceOptions.ConfigDirPath);

        EnsureConfigDirExists();

        string configFilePath = Path.Combine(_configServiceOptions.ConfigDirPath, "config.json");

        _logger.LogInformation("Ensuring config file exists at '{configFilePath}'...", configFilePath);

        await GetCsvImporterConfigAsync();
    }

    public async Task SaveConfigAsync() => await SaveCsvImporterConfigAsync();

    /// <summary>
    /// Gets the config for the CsvImporter tool.
    /// </summary>
    /// <returns>The config for the CsvImporter tool.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the config file.</exception>
    private async Task GetCsvImporterConfigAsync()
    {
        EnsureConfigDirExists();

        string configFilePath = Path.Combine(_configServiceOptions.ConfigDirPath, "config.json");

        if (!File.Exists(configFilePath))
        {
            Config = new();
            await SaveCsvImporterConfigAsync();

            _logger.LogWarning("Please update the config file at '{configFilePath}' and re-run the tool.", configFilePath);

            Environment.Exit(1);
        }
        else
        {
            string configJson = await File.ReadAllTextAsync(configFilePath);
            Config = JsonSerializer.Deserialize(
                json: configJson,
                jsonTypeInfo: ConfigJsonContext.Default.CsvImporterConfig
            ) ?? throw new Exception($"Unable to deserialize config file: {configFilePath}");
        }
    }

    /// <summary>
    /// Saves the config for the CsvImporter tool.
    /// </summary>
    private async Task SaveCsvImporterConfigAsync()
    {
        if (Config is null)
        {
            throw new NullReferenceException("Config is null");
        }

        EnsureConfigDirExists();

        string configFilePath = Path.Combine(_configServiceOptions.ConfigDirPath, "config.json");

        string configJson = JsonSerializer.Serialize(
                value: Config,
                jsonTypeInfo: ConfigJsonContext.Default.CsvImporterConfig
            );

        await File.WriteAllTextAsync(configFilePath, configJson);
    }

    private void EnsureConfigDirExists()
    {
        if (!Directory.Exists(_configServiceOptions.ConfigDirPath))
        {
            Directory.CreateDirectory(_configServiceOptions.ConfigDirPath);
        }
    }
}
