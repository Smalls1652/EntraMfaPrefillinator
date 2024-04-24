using System.Diagnostics;
using System.Text.Json;

using EntraMfaPrefillinator.Lib.Azure.Services;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

public sealed class MainService : IMainService, IHostedService, IDisposable
{
    private bool _disposed;
    private CancellationTokenSource? _cts;
    private Task? _runTask;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.Tools.CsvImporter.MainService");
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    private readonly IQueueClientService _queueClientService;
    private readonly IDbContextFactory<UserDetailsDbContext> _dbContextFactory;
    private readonly ICsvFileReaderService _csvFileReader;
    private readonly MainServiceOptions _options;

    public MainService(IHostApplicationLifetime appLifetime, ILoggerFactory loggerFactory, IConfiguration configuration, IQueueClientService queueClientService, IDbContextFactory<UserDetailsDbContext> dbContextFactory, ICsvFileReaderService csvFileReader, IOptions<MainServiceOptions> options)
    {
        _appLifetime = appLifetime;
        _logger = loggerFactory.CreateLogger("MainService");
        _configuration = configuration;
        _queueClientService = queueClientService;
        _dbContextFactory = dbContextFactory;
        _csvFileReader = csvFileReader;
        _options = options.Value;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(
            name: "RunAsync",
            kind: ActivityKind.Server
        );

        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {

            CsvImporterConfigFile configFile = _configuration.Get<CsvImporterConfigFile>()!;

            int maxTasks = 256;

            string runningDir = Environment.CurrentDirectory;

            bool isDeltaRun = false;

            using UserDetailsDbContext dbContext = _dbContextFactory.CreateDbContext();

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

                throw csvNotFoundException;
            }

            if (csvFileInfo.Extension != ".csv")
            {
                ArgumentException csvFileExtensionException = new("File must be a CSV file", csvFileInfo.FullName);
                _logger.LogError(csvFileExtensionException, "Error reading CSV file");

                throw csvFileExtensionException;
            }

            _logger.LogInformation("Reading CSV file: {CsvFilePath}", csvFileInfo.FullName);

            // Read CSV file.
            List<UserDetails> userDetailsList;
            try
            {
                userDetailsList = await _csvFileReader.ReadCsvFileAsync(
                    csvFilePath: csvFileInfo.FullName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading CSV file");

                return;
            }

            activity?.SetTag("user.foundInCsv.count", userDetailsList.Count); ;
            _logger.LogInformation("Found {UserDetailsCount} users in CSV file", userDetailsList.Count);
            
            // Get the current count of users in the database.
            int lastRunUserDetailsCount = await dbContext.UserDetails.CountAsync(cancellationToken);

            // If there are users in the database, run the delta against the current list.
            
            if (lastRunUserDetailsCount != 0)
            {
                isDeltaRun = true;
                List<UserDetails> deltaList = [];
                _logger.LogInformation("Getting delta between current CSV file and last run CSV file...");
                Stopwatch deltaStopwatch = Stopwatch.StartNew();

                try
                {
                    deltaList = await _csvFileReader.GetDeltaAsync(
                        currentList: userDetailsList,
                        cancellationToken: cancellationToken
                    );
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError("Delta operation was cancelled during execution.");

                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting delta between current CSV file and last run CSV file.");

                    return;
                }

                activity?.SetTag("user.delta.count", deltaList.Count);
                _logger.LogInformation("Filtered down to {DeltaListCount} users not in last run CSV file", deltaList.Count);
                userDetailsList = deltaList;

                deltaStopwatch.Stop();
                _logger.LogInformation("Delta completed in {DeltaElapsedMilliseconds}ms", deltaStopwatch.ElapsedMilliseconds);
            }


            // Filter out users without an email or phone number set.
            List<UserDetails> filteredUserDetailsList = userDetailsList.FindAll(userDetails => userDetails.SecondaryEmail is not null || userDetails.PhoneNumber is not null || userDetails.HomePhoneNumber is not null);

            activity?.SetTag("user.filtered.count", filteredUserDetailsList.Count);
            _logger.LogInformation("Filtered to {FilteredUserDetailsCount} users with email or phone number", filteredUserDetailsList.Count);

            // If there are no users to process, exit.
            if (filteredUserDetailsList.Count == 0)
            {
                _logger.LogWarning("No users to process, exiting");

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
                if (configFile.Config.DryRunEnabled)
                {
                    continue;
                }
                Task newQueueItemTask;
                try
                {
                    newQueueItemTask = SendUserAuthUpdateQueueItemAsync(semaphoreSlim, userItem, isDeltaRun, configFile);
                    tasks.Add(newQueueItemTask);
                    //_logger.LogInformation("Sent message to queue for '{UserName}'.", userItem.UserName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending message to queue for '{UserName}'.", userItem.UserName);
                    throw;
                }
            }

            // Wait for all tasks to complete.
            _logger.LogInformation("Waiting for tasks to complete...");
            await Task.WhenAll(tasks);

            _logger.LogInformation("Updating database...");
            if (lastRunUserDetailsCount == 0)
            {
                foreach (var userItem in userDetailsList)
                {
                    userItem.LastUpdated = DateTimeOffset.UtcNow;
                    dbContext.UserDetails.Add(userItem);
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                foreach (var userItem in userDetailsList)
                {
                    userItem.LastUpdated = DateTimeOffset.UtcNow;

                    if (userItem.IsInLastRun)
                    {
                        dbContext.UserDetails.Update(userItem);
                    }
                    else
                    {
                        dbContext.UserDetails.Add(userItem);
                    }
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (configFile.Config.DryRunEnabled)
            {
                _logger.LogWarning("Dry run, exiting");

                return;
            }

            // Update the config file.
            configFile.Config.LastRunDateTime = DateTimeOffset.UtcNow;
            await ConfigFileUtils.SaveConfigAsync(configFile, _options.ConfigFilePath);

            return;
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation("Completed in {ElapsedMilliseconds}ms", stopwatch.ElapsedMilliseconds);

            _appLifetime.StopApplication();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _runTask = RunAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_runTask is not null)
        {
            try
            {
                _cts?.Cancel();
            }
            finally
            {
                await _runTask
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        }
    }

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _cts?.Dispose();
        _activitySource.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    private async Task SendUserAuthUpdateQueueItemAsync(SemaphoreSlim semaphoreSlim, UserDetails userDetails, bool isDeltaRun, CsvImporterConfigFile configFile)
    {
        // Wait for the semaphore to be available before sending the message.
        await semaphoreSlim.WaitAsync();

        try
        {
            UserAuthUpdateQueueItem userAuthUpdate = new()
            {
                EmployeeId = userDetails.EmployeeNumber,
                UserName = userDetails.UserName,
                EmailAddress = userDetails.SecondaryEmail,
                PhoneNumber = userDetails.PhoneNumber,
                HomePhone = userDetails.HomePhoneNumber
            };

            // Serialize the user auth update to JSON and send it to the queue.
            string userItemJson = JsonSerializer.Serialize(
                value: userAuthUpdate,
                jsonTypeInfo: CoreJsonContext.Default.UserAuthUpdateQueueItem
            );

            try
            {
                await _queueClientService.AuthUpdateQueueClient.SendMessageAsync(
                    messageText: userItemJson,
                    timeToLive: isDeltaRun ? configFile.Config.QueueMessageTTL.DeltaRuns : configFile.Config.QueueMessageTTL.FirstRun
                );
            }
            catch (Exception)
            {
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
