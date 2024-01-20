using System.Text;
using System.Text.Json;
using EntraMfaPrefillinator.Tools.CsvImporter;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions.QueueClient;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions.ServiceSetup;
using EntraMfaPrefillinator.Tools.CsvImporter.Logging;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Models.Exceptions;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

ILoggerFactory preLaunchLoggerFactory = LoggerFactory.Create(configure =>
{
    configure.AddSimpleConsole(options =>
    {
        options.SingleLine = false;
        options.UseUtcTimestamp = true;
    });
});

ILogger preLaunchLogger = preLaunchLoggerFactory.CreateLogger("EntraMfaPrefillinator.Tools.CsvImporter.PreLaunch");

string configDirPath = Path.Combine(
    path1: Environment.IsPrivilegedProcess
        ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    path2: "EntraMfaPrefillinator",
    path3: "CsvImporter"
);

string logsDirPath = Path.Combine(
    path1: configDirPath,
    path2: "Logs"
);

if (!Directory.Exists(configDirPath))
{
    preLaunchLogger.LogInformation("Config directory does not exist. Creating...");
    Directory.CreateDirectory(configDirPath);
}

if (!Directory.Exists(logsDirPath))
{
    preLaunchLogger.LogInformation("Logs directory does not exist. Creating...");
    Directory.CreateDirectory(logsDirPath);
}

string configFilePath = Path.Combine(configDirPath, "config.json");
string logFilePath = Path.Combine(logsDirPath, $"CsvImporter_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log");

bool configFileExists = File.Exists(configFilePath);

if (!configFileExists)
{
    preLaunchLogger.LogWarning("Config file does not exist. Creating...");
    CsvImporterConfigFile configFile = new();
    await File.WriteAllTextAsync(
        path: configFilePath,
        contents: JsonSerializer.Serialize(
            value: configFile,
            jsonTypeInfo: ConfigJsonContext.Default.CsvImporterConfigFile
        )
    );

    StringBuilder configFileNewlyCreatedMessage = new();
    configFileNewlyCreatedMessage.AppendLine("Config file created. Please edit the config file and re-run the tool.");
    configFileNewlyCreatedMessage.AppendLine($"Config file path: {configFilePath}");
    configFileNewlyCreatedMessage.AppendLine();
    configFileNewlyCreatedMessage.AppendLine("Please ensure the following options are set:");
    configFileNewlyCreatedMessage.AppendLine("- csvFilePath");
    configFileNewlyCreatedMessage.AppendLine("- queueUri");

    preLaunchLogger.LogError("{ConfigFileNewlyCreatedMessage}", configFileNewlyCreatedMessage.ToString());
    preLaunchLoggerFactory.Dispose();
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile(
        path: configFilePath,
        optional: false,
        reloadOnChange: true
    );

CsvImporterConfig csvImporterConfig;
try
{
    csvImporterConfig = builder.Configuration.GetSection("config").Get<CsvImporterConfig>() ?? throw new NullReferenceException("Config is empty.");
}
catch (NullReferenceException ex)
{
    preLaunchLogger.LogError(ex, "The 'config' section of the config file is empty.");
    preLaunchLoggerFactory.Dispose();
    return;
}

try
{
    preLaunchLogger.LogInformation("Validating config...");
    ConfigFileUtils.EnsureRequiredOptionsAreSet(csvImporterConfig);
}
catch (ConfigPropertyException ex)
{
    preLaunchLogger.LogError(ex, "Required option '{OptionName}' is not set.", ex.PropertyName);
    preLaunchLoggerFactory.Dispose();
    return;
}

builder.Logging
    .ClearProviders()
    .AddSimpleConsole(options =>
    {
        options.SingleLine = false;
        options.UseUtcTimestamp = true;
    })
    .AddFileLogger(options =>
    {
        options.FilePath = logFilePath;
    });

try
{
    preLaunchLogger.LogInformation("Configuring queue client service...");
    builder.Services
        .AddCsvImporterQueueClientService(
            queueUri: csvImporterConfig.QueueUri!,
            tokenCredential: AuthUtils.CreateTokenCredential(csvImporterConfig.QueueUri!)
        );
}
catch (Exception ex)
{
    preLaunchLogger.LogError(ex, "Failed to configure queue client service.");
    preLaunchLoggerFactory.Dispose();
    throw;
}

builder.Services
    .AddMainService(options =>
    {
        options.ConfigFilePath = configFilePath;
        options.ConfigDirPath = configDirPath;
    });

preLaunchLoggerFactory.Dispose();

using var host = builder.Build();

await host.RunAsync();