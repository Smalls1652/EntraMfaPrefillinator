using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for reading CSV files.
/// </summary>
internal static class CsvFileReader
{
    /// <summary>
    /// Reads a CSV file and parses the data.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="UserDetails"/> objects.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the CSV file.</exception>
    public static async Task<List<UserDetails>> ReadCsvFileAsync(ILogger logger, string csvFilePath)
    {
        using StreamReader csvFileReader = new(File.OpenRead(csvFilePath));

        List<UserDetails> userDetailsList = [];

        string? csvLine;
        int currentLine = 0;
        while ((csvLine = await csvFileReader.ReadLineAsync()) is not null)
        {
            bool isValidCsvLine = CsvDataRegexTools.IsValidCsvLine(csvLine);

            // Skip the header line if it is the first line.
            // If the first line is not the header line, throw an exception.
            if (isValidCsvLine && currentLine == 0)
            {
                currentLine++;
                continue;
            }
            else if (!isValidCsvLine && currentLine == 0)
            {
                throw new Exception($"Invalid CSV line: {csvLine}");
            }

            // If the line is valid, add it to the list.
            if (isValidCsvLine)
            {
                userDetailsList.Add(new(csvLine));
            }
            else
            {
                logger.LogWarning("Invalid CSV line: {csvLine}", csvLine);
            }
        }

        return userDetailsList;
    }

    /// <summary>
    /// Gets the delta between the current list and the last run list.
    /// </summary>
    /// <param name="currentList">The current list.</param>
    /// <param name="lastRunList">The last run list.</param>
    /// <param name="maxTasks">The maximum number of tasks to run at once.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="UserDetails"/> objects that represent the delta.</returns>
    public static async Task<List<UserDetails>> GetDeltaAsync(List<UserDetails> currentList, List<UserDetails> lastRunList, int maxTasks = 5, CancellationToken cancellationToken = default)
    {
        double initialTasksCount = Math.Round((double)(maxTasks / 2), 0);

        using SemaphoreSlim semaphoreSlim = new(
            initialCount: (int)initialTasksCount,
            maxCount: maxTasks
        );

        List<UserDetails> deltaList = [];
        List<Task<UserDetails?>> deltaTasks = [];

        foreach (var userDetailsItem in currentList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            var deltaTask = Task.Run(async () =>
            {
                await semaphoreSlim.WaitAsync(cancellationToken);

                try
                {
                    UserDetails? lastRunUserDetailsItem = lastRunList.Find(item => item.UserName == userDetailsItem.UserName);

                    // If the user was not found in the last run CSV file,
                    // add the user to the delta list.
                    if (lastRunUserDetailsItem is null)
                    {
                        return userDetailsItem;
                    }

                    // If the user was found in the last run CSV file and the email or phone number has changed,
                    // add the user to the delta list.
                    if (lastRunUserDetailsItem.PhoneNumber != userDetailsItem.PhoneNumber || lastRunUserDetailsItem.SecondaryEmail != userDetailsItem.SecondaryEmail)
                    {
                        return userDetailsItem;
                    }

                    return null;
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            }, cancellationToken);

            deltaTasks.Add(deltaTask);
        }

        try
        {
            await Task.WhenAll(deltaTasks);
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        foreach (var deltaTask in deltaTasks)
        {
            UserDetails? deltaTaskResult = deltaTask.Result;

            if (deltaTaskResult is not null)
            {
                deltaList.Add(deltaTaskResult);
            }

            deltaTask.Dispose();
        }

        return deltaList;
    }
}