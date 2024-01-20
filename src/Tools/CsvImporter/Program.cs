using System.Text.Json;
using EntraMfaPrefillinator.Tools.CsvImporter;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions;
using EntraMfaPrefillinator.Tools.CsvImporter.Extensions.QueueClient;
using EntraMfaPrefillinator.Tools.CsvImporter.Logging;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Services;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
    Directory.CreateDirectory(configDirPath);
}

if (!Directory.Exists(logsDirPath))
{
    Directory.CreateDirectory(logsDirPath);
}

string configFilePath = Path.Combine(configDirPath, "config.json");
string logFilePath = Path.Combine(logsDirPath, $"CsvImporter_{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}.log");

bool configFileExists = File.Exists(configFilePath);

CsvImporterConfig? preLaunchConfig = null;
if (configFileExists)
{
    preLaunchConfig = JsonSerializer.Deserialize(
        json: File.ReadAllText(configFilePath),
        jsonTypeInfo: CoreJsonContext.Default.CsvImporterConfig
    );
}

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddEnvironmentVariables();

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

if (preLaunchConfig is not null)
{
    try
    {
        builder.Services
            .AddCsvImporterQueueClientService(
                queueUri: Environment.GetEnvironmentVariable("QUEUE_URI") ?? preLaunchConfig.QueueUri ?? throw new NullReferenceException("QUEUE_URI environment variable not set or missing from config file."),
                tokenCredential: AuthUtils.CreateTokenCredential(Environment.GetEnvironmentVariable("QUEUE_URI") ?? preLaunchConfig.QueueUri)
            );
    }
    catch (Exception)
    {
        throw;
    }
}

builder.Services
    .AddConfigService(options =>
    {
        options.ConfigDirPath = configDirPath;
    });

if (configFileExists)
{
    builder.Services
        .AddHostedService<MainService>();
}

using var host = builder.Build();

if (!configFileExists)
{
    var configService = host.Services.GetRequiredService<IConfigService>();

    await configService.LoadConfigAsync();
}

await host.RunAsync();