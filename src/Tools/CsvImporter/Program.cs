using System.Text;
using System.Text.Json;
using EntraMfaPrefillinator.Lib.Azure.Extensions;
using EntraMfaPrefillinator.Tools.CsvImporter;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions.ServiceSetup;
using EntraMfaPrefillinator.Tools.CsvImporter.Logging;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Models.Exceptions;
using EntraMfaPrefillinator.Tools.CsvImporter.Services;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;
using EntraMfaPrefillinator.Hosting;
using EntraMfaPrefillinator.Hosting.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions.Telemetry;

// Create a logger factory for pre-launch logging.
ILoggerFactory preLaunchLoggerFactory = LoggerFactory.Create(configure =>
{
    configure.AddSimpleConsole(options =>
    {
        options.SingleLine = false;
        options.UseUtcTimestamp = true;
    });
});

ILogger preLaunchLogger = preLaunchLoggerFactory.CreateLogger("PreLaunch");

// Get the path to the config directory and create it if it doesn't exist.
string configDirPath = Path.Combine(
    path1: Environment.IsPrivilegedProcess
        ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
        : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    path2: "EntraMfaPrefillinator",
    path3: "CsvImporter"
);

if (!Directory.Exists(configDirPath))
{
    preLaunchLogger.LogInformation("Config directory does not exist. Creating...");
    Directory.CreateDirectory(configDirPath);
}

// Get the path to the logs directory and create it if it doesn't exist.
string logsDirPath = Path.Combine(
    path1: configDirPath,
    path2: "Logs"
);

string dbPath = Path.Combine(
    path1: configDirPath,
    path2: "CsvImporter.db"
);

if (!Directory.Exists(logsDirPath))
{
    preLaunchLogger.LogInformation("Logs directory does not exist. Creating...");
    Directory.CreateDirectory(logsDirPath);
}

// Get the path to the config file.
// If the config file doesn't exist, create it and exit.
string configFilePath = Path.Combine(configDirPath, "config.json");

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

// Get the path to the log file for this run.
string logFilePath = Path.Combine(logsDirPath, $"CsvImporter_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log");

preLaunchLogger.LogInformation("Creating host...");

// Start building the host.
var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddSlimHostLifetime();

// Add the configuration for the host.
builder.Configuration
    .AddEnvironmentVariables()
    .AddJsonFile(
        path: configFilePath,
        optional: false,
        reloadOnChange: true
    );

// Get the values from the config file.
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

// Validate the config to ensure required options are set.
try
{
    ConfigFileUtils.EnsureRequiredOptionsAreSet(csvImporterConfig);
}
catch (ConfigPropertyException ex)
{
    preLaunchLogger.LogError(ex, "Required option '{OptionName}' is not set.", ex.PropertyName);
    preLaunchLoggerFactory.Dispose();
    return;
}

// Configure logging for the host.
builder.Logging
    .AddSimpleConsole(options =>
    {
        options.SingleLine = false;
        options.UseUtcTimestamp = true;
    });

if (csvImporterConfig.LoggingAndTelemetry.EnableFileLogging)
{
    builder.Logging
        .AddFileLogger(options =>
        {
            options.FilePath = logFilePath;
        });
}

// Configure the basic OpenTelemetry logging and telemetry without Azure Application Insights.
if (csvImporterConfig.LoggingAndTelemetry.EnableOpenTelemetry && !csvImporterConfig.LoggingAndTelemetry.EnableOpenTelelmetryAzureAppInsights)
{
    preLaunchLogger.LogInformation("Configuring OpenTelemetry logging and telemetry...");
    builder.Logging
        .AddOpenTelemetryLogging(csvImporterConfig.LoggingAndTelemetry.OpenTelemetryInstanceId);

    builder.Services
        .AddOpenTelemetryMetricsAndTracing(csvImporterConfig.LoggingAndTelemetry.OpenTelemetryInstanceId);
}

if (csvImporterConfig.LoggingAndTelemetry.EnableOpenTelemetry && csvImporterConfig.LoggingAndTelemetry.EnableOpenTelelmetryAzureAppInsights)
{
    preLaunchLogger.LogInformation("Configuring OpenTelemetry logging and telemetry with Azure Application Insights...");

    preLaunchLogger.LogInformation("Instance ID: {OpenTelemetryInstanceId}", csvImporterConfig.LoggingAndTelemetry.OpenTelemetryInstanceId);
    preLaunchLogger.LogInformation("Token credential enabled: {EnableOpenTelemetryAzureTokenCredential}", csvImporterConfig.LoggingAndTelemetry.EnableOpenTelemetryAzureTokenCredential);

    builder.Logging
        .AddOpenTelemetryLogging(
            instanceId: csvImporterConfig.LoggingAndTelemetry.OpenTelemetryInstanceId,
            azureAppInsightsConnectionString: csvImporterConfig.LoggingAndTelemetry.AzureAppInsightsConnectionString ?? throw new NullReferenceException("Azure Application Insights connection string is null."),
            tokenCredential: csvImporterConfig.LoggingAndTelemetry.EnableOpenTelemetryAzureTokenCredential ? AuthUtils.CreateGeneralTokenCredential() : null
        );

    builder.Services
        .AddOpenTelemetryMetricsAndTracing(
            instanceId: csvImporterConfig.LoggingAndTelemetry.OpenTelemetryInstanceId,
            azureAppInsightsConnectionString: csvImporterConfig.LoggingAndTelemetry.AzureAppInsightsConnectionString ?? throw new NullReferenceException("Azure Application Insights connection string is null."),
            tokenCredential: csvImporterConfig.LoggingAndTelemetry.EnableOpenTelemetryAzureTokenCredential ? AuthUtils.CreateGeneralTokenCredential() : null
        );

}

// Add the MainService to the host.
// This service will be automatically run when the host is started.
builder.Services
    .AddMainService(options =>
    {
        options.ConfigFilePath = configFilePath;
        options.ConfigDirPath = configDirPath;
    });

// Add the QueueClientService to the host.
try
{
    if (builder.Configuration.GetValue<bool>("USE_LOCAL_QUEUE"))
    {
        builder.Services
            .AddQueueClientService();
    }
    else
    {
        builder.Services
            .AddQueueClientService(
                queueUri: csvImporterConfig.QueueUri!,
                tokenCredential: AuthUtils.CreateQueueTokenCredential(csvImporterConfig.QueueUri!)
        );
    }
}
catch (Exception ex)
{
    preLaunchLogger.LogError(ex, "Failed to configure queue client service.");
    preLaunchLoggerFactory.Dispose();
    throw;
}

builder.Services
    .AddCsvImporterSqliteService(options =>
    {
        options.DbPath = dbPath;
    })
    .AddCsvFileReaderService();

// Dispose the pre-launch logger factory.
preLaunchLoggerFactory.Dispose();

// Build the host.
using var host = builder.Build();

var dbService = host.Services.GetRequiredService<ICsvImporterSqliteService>();

await dbService.InitializeDatabaseAsync();

// Run the host.
await host.RunAsync();