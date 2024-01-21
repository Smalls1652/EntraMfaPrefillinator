using System.Diagnostics;
using System.Text.Json;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Services;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

public class MainService : IMainService, IHostedService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<MainService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IQueueClientService _queueClientService;
    private readonly MainServiceOptions _options;

    public MainService(IHostApplicationLifetime appLifetime, ILogger<MainService> logger, IConfiguration configuration, IQueueClientService queueClientService, IOptions<MainServiceOptions> options)
    {
        _appLifetime = appLifetime;
        _logger = logger is not null ? logger : NullLogger<MainService>.Instance;
        _configuration = configuration;
        _queueClientService = queueClientService;
        _options = options.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        int exitCode = 0;

        try
        {

            CsvImporterConfigFile configFile = _configuration.Get<CsvImporterConfigFile>()!;

            int maxTasks = 256;

            string runningDir = Environment.CurrentDirectory;

            // Resolve CSV file path.
            _logger.LogInformation("Reading CSV file from {CsvFilePath}", configFile.Config.CsvFilePath);
            string relativePathToCsv = Path.GetRelativePath(
                relativeTo: runningDir,
                path: configFile.Config.CsvFilePath
            );
            FileInfo csvFileInfo = new(relativePathToCsv);

            if (!csvFileInfo.Exists)
            {
                FileNotFoundException csvNotFoundException = new("File not found", csvFileInfo.FullName);
                _logger.LogError(csvNotFoundException, "Error reading CSV file");
                exitCode = 50;

                throw csvNotFoundException;
            }

            if (csvFileInfo.Extension != ".csv")
            {
                ArgumentException csvFileExtensionException = new("File must be a CSV file", csvFileInfo.FullName);
                _logger.LogError(csvFileExtensionException, "Error reading CSV file");
                exitCode = 51;

                throw csvFileExtensionException;
            }

            _logger.LogInformation("Reading CSV file: {CsvFilePath}", csvFileInfo.FullName);

            // Read CSV file.
            List<UserDetails> userDetailsList;
            try
            {
                userDetailsList = await CsvFileReader.ReadCsvFileAsync(
                    logger: _logger,
                    csvFilePath: csvFileInfo.FullName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file");
                exitCode = 30;

                return;
            }

            _logger.LogInformation("Found {UserDetailsCount} users in CSV file", userDetailsList.Count);

            // If last run CSV file path exists, 
            // read the last run CSV file.
            List<UserDetails>? lastRunUserDetailsList = null;
            if (configFile.Config.LastCsvPath is not null)
            {
                Path.GetRelativePath(
                    relativeTo: runningDir,
                    path: configFile.Config.LastCsvPath
                );

                FileInfo lastCsvFileInfo = new(configFile.Config.LastCsvPath);

                if (lastCsvFileInfo.Exists)
                {
                    _logger.LogInformation("Reading last run CSV file: {LastCsvFilePath}", lastCsvFileInfo.FullName);
                    lastRunUserDetailsList = await CsvFileReader.ReadCsvFileAsync(
                        logger: _logger,
                        csvFilePath: lastCsvFileInfo.FullName
                    );
                }
            }

            // If last run CSV file was read,
            // get the delta between the last run CSV file and the current CSV file.
            if (lastRunUserDetailsList is not null && lastRunUserDetailsList.Count != 0)
            {
                Stopwatch deltaStopwatch = Stopwatch.StartNew();

                _logger.LogInformation("Found {LastRunUserDetailsCount} users in last run CSV file", lastRunUserDetailsList.Count);

                List<UserDetails> deltaList = [];
                try
                {
                    deltaList = await CsvFileReader.GetDeltaAsync(
                        currentList: userDetailsList,
                        lastRunList: lastRunUserDetailsList,
                        maxTasks: 10,
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Delta operation was cancelled during execution.");
                    exitCode = 40;

                    Environment.Exit(exitCode);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting delta between current CSV file and last run CSV file.");
                    exitCode = 41;

                    Environment.Exit(exitCode);
                }

                _logger.LogInformation("Filtered down to {DeltaListCount} users not in last run CSV file", deltaList.Count);

                // Update the user details list to the delta list.
                userDetailsList = deltaList;

                deltaStopwatch.Stop();
                _logger.LogInformation("Delta completed in {DeltaElapsedMilliseconds}ms", deltaStopwatch.ElapsedMilliseconds);
            }

            // Filter out users without an email or phone number set.
            List<UserDetails> filteredUserDetailsList = userDetailsList.FindAll(
                match: userDetails => userDetails.SecondaryEmail is not null || userDetails.PhoneNumber is not null
            );

            _logger.LogInformation("Filtered to {FilteredUserDetailsCount} users with email or phone number", filteredUserDetailsList.Count);

            // If there are no users to process, exit.
            if (filteredUserDetailsList.Count == 0)
            {
                _logger.LogWarning("No users to process, exiting");
                exitCode = 10;

                return;
            }

            // If this is a dry run, exit.
            if (configFile.Config.DryRunEnabled)
            {
                _logger.LogWarning("Dry run, exiting");
                exitCode = 5;

                return;
            }

            // Set the initial semaphore count to half the max tasks.
            double initialTasksCount = Math.Round((double)(maxTasks / 2), 0);

            using SemaphoreSlim semaphoreSlim = new(
                initialCount: (int)initialTasksCount,
                maxCount: maxTasks
            );

            // Process each item and send to queue.
            List<Task> tasks = [];
            foreach (var userItem in filteredUserDetailsList)
            {
                UserAuthUpdateQueueItem queueItem = new()
                {
                    EmployeeId = userItem.EmployeeNumber,
                    UserName = userItem.UserName,
                    EmailAddress = userItem.SecondaryEmail,
                    PhoneNumber = userItem.PhoneNumber
                };

                var newQueueItemTask = SendUserAuthUpdateQueueItemAsync(semaphoreSlim, queueItem);

                tasks.Add(newQueueItemTask);
            }

            // Wait for all tasks to complete.
            _logger.LogInformation("Waiting for tasks to complete...");
            await Task.WhenAll(tasks);

            // Copy the CSV file used for this run to the config directory.
            _logger.LogInformation("Saving last run CSV file path to config file.");
            string copiedCsvFilePath = Path.Combine(_options.ConfigDirPath, "lastRun.csv");
            File.Copy(
                sourceFileName: csvFileInfo.FullName,
                destFileName: copiedCsvFilePath,
                overwrite: true
            );

            // Update the config file.
            configFile.Config.LastCsvPath = copiedCsvFilePath;
            configFile.Config.LastRunDateTime = DateTimeOffset.UtcNow;
            await ConfigFileUtils.SaveConfigAsync(configFile, _options.ConfigFilePath);

            exitCode = 0;
            return;
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation("Completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

            _appLifetime.StopApplication();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken);

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MainService...");
        
        await Task.CompletedTask;
    }

    private async Task SendUserAuthUpdateQueueItemAsync(SemaphoreSlim semaphoreSlim, UserAuthUpdateQueueItem userAuthUpdate)
    {
        // Wait for the semaphore to be available before sending the message.
        await semaphoreSlim.WaitAsync();

        try
        {
            // Serialize the user auth update to JSON and send it to the queue.
            string userItemJson = JsonSerializer.Serialize(
                value: userAuthUpdate,
                jsonTypeInfo: CoreJsonContext.Default.UserAuthUpdateQueueItem
            );

            try
            {
                await _queueClientService.AuthUpdateQueueClient.SendMessageAsync(
                    messageText: userItemJson
                );
                _logger.LogInformation("Sent message to queue for '{userAuthUpdate.UserName}'.", userAuthUpdate.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to queue for '{userAuthUpdate.UserName}'.", userAuthUpdate.UserName);
                throw;

            }
        }
        finally
        {
            // Release the semaphore when the message has been sent.
            semaphoreSlim.Release();
        }

        return;
    }
}
