using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Services;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

Stopwatch stopwatch = Stopwatch.StartNew();

// Set Azure Storage connection string and dry run flag from environment variables.
string storageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") ?? throw new Exception("STORAGE_CONNECTION_STRING environment variable not set");

// Set CSV file path and max tasks from command line arguments.
string csvFilePathArg;
try
{
    csvFilePathArg = args[0];
}
catch (Exception)
{
    throw new ArgumentException("Missing CSV file path argument");
}

string maxTasksArg;
try
{
    maxTasksArg = args[1];
}
catch (Exception)
{
    maxTasksArg = "256";
}

int maxTasks;
try
{
    maxTasks = int.Parse(maxTasksArg);
}
catch (Exception ex)
{
    throw new ArgumentException($"Invalid max tasks argument: {maxTasksArg}", ex);
}

// Read config file.
CsvImporterConfig csvImporterConfig;
try
{
    csvImporterConfig = await ConfigFileUtils.GetCsvImporterConfigAsync();
}
catch (Exception ex)
{
    throw new Exception($"Error reading config file: {ex.Message}");
}

string runningDir = Environment.CurrentDirectory;
ConsoleUtils.WriteInfo($"Running from {runningDir}");

// Resolve CSV file path.
ConsoleUtils.WriteInfo($"Reading CSV file from {csvFilePathArg}");
string relativePathToCsv = Path.GetRelativePath(
    relativeTo: runningDir,
    path: csvFilePathArg
);

ConsoleUtils.WriteInfo($"Relative path to CSV file: {relativePathToCsv}");
FileInfo csvFileInfo = new(relativePathToCsv);

if (!csvFileInfo.Exists)
{
    throw new FileNotFoundException("File not found", csvFileInfo.FullName);
}

if (csvFileInfo.Extension != ".csv")
{
    throw new ArgumentException("File must be a CSV file", csvFileInfo.FullName);
}

ConsoleUtils.WriteInfo($"Reading CSV file: {csvFileInfo.FullName}");

// Read CSV file.
List<UserDetails> userDetailsList;
try
{
    userDetailsList = await CsvFileReader.ReadCsvFileAsync(
        csvFilePath: csvFileInfo.FullName
    );
}
catch (Exception)
{
    throw;
}

ConsoleUtils.WriteInfo($"Found {userDetailsList.Count} users in CSV file");

// If last run CSV file path exists, 
// read the last run CSV file.
List<UserDetails>? lastRunUserDetailsList = null;
if (csvImporterConfig.LastCsvPath is not null)
{
    Path.GetRelativePath(
        relativeTo: runningDir,
        path: csvImporterConfig.LastCsvPath
    );

    FileInfo lastCsvFileInfo = new(csvImporterConfig.LastCsvPath);

    if (lastCsvFileInfo.Exists)
    {
        ConsoleUtils.WriteInfo($"Reading last run CSV file: {lastCsvFileInfo.FullName}");
        lastRunUserDetailsList = await CsvFileReader.ReadCsvFileAsync(
            csvFilePath: lastCsvFileInfo.FullName
        );
    }
}

// If last run CSV file was read,
// get the delta between the last run CSV file and the current CSV file.
if (lastRunUserDetailsList is not null && lastRunUserDetailsList.Count != 0)
{
    ConsoleUtils.WriteInfo($"Found {lastRunUserDetailsList.Count} users in last run CSV file");

    List<UserDetails> deltaList = [];

    foreach (var userDetailsItem in userDetailsList)
    {
        UserDetails? lastRunUserDetailsItem = lastRunUserDetailsList.Find(item => item.UserName == userDetailsItem.UserName);

        // If the user was not found in the last run CSV file,
        // add the user to the delta list.
        if (lastRunUserDetailsItem is null)
        {
            deltaList.Add(userDetailsItem);
            continue;
        }

        // If the user was found in the last run CSV file and the email or phone number has changed,
        // add the user to the delta list.
        if (lastRunUserDetailsItem.PhoneNumber != userDetailsItem.PhoneNumber || lastRunUserDetailsItem.SecondaryEmail != userDetailsItem.SecondaryEmail)
        {
            deltaList.Add(userDetailsItem);
            continue;
        }
    }

    ConsoleUtils.WriteInfo($"Filtered down to {deltaList.Count} users not in last run CSV file");

    // Update the user details list to the delta list.
    userDetailsList = deltaList;
}

// Filter out users without an email or phone number set.
List<UserDetails> filteredUserDetailsList = userDetailsList.FindAll(
    match: userDetails => userDetails.SecondaryEmail is not null || userDetails.PhoneNumber is not null
);

ConsoleUtils.WriteInfo($"Filtered to {filteredUserDetailsList.Count} users with email or phone number");

// If there are no users to process, exit.
if (filteredUserDetailsList.Count == 0)
{
    ConsoleUtils.WriteInfo($"No users to process, exiting");
    return;
}

// If this is a dry run, exit.
if (csvImporterConfig.DryRunEnabled)
{
    ConsoleUtils.WriteInfo($"Dry run, exiting");
    return;
}

// Configure Azure Storage Queue client service.
QueueClientService queueClientService = new(
    connectionString: storageConnectionString
);

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

    var newQueueItemTask = QueueClientUtils.SendUserAuthUpdateQueueItemAsync(queueClientService, semaphoreSlim, queueItem);

    tasks.Add(newQueueItemTask);
}

// Wait for all tasks to complete.
ConsoleUtils.WriteInfo($"Waiting for tasks to complete...");
await Task.WhenAll(tasks);

// Copy the CSV file used for this run to the config directory.
ConsoleUtils.WriteInfo($"Saving last run CSV file path to config file");
string copiedCsvFilePath = Path.Combine(ConfigFileUtils.GetConfigDirPath(), "lastRun.csv");
File.Copy(
    sourceFileName: csvFileInfo.FullName,
    destFileName: copiedCsvFilePath,
    overwrite: true
);

// Update the config file.
csvImporterConfig.LastCsvPath = copiedCsvFilePath;
csvImporterConfig.LastRunDateTime = DateTimeOffset.UtcNow;

await ConfigFileUtils.SaveCsvImporterConfigAsync(csvImporterConfig);

stopwatch.Stop();

ConsoleUtils.WriteInfo($"Completed in {stopwatch.ElapsedMilliseconds}ms");
