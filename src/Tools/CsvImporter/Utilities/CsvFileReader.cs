using EntraMfaPrefillinator.Tools.CsvImporter.Models;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

/// <summary>
/// Houses methods for reading CSV files.
/// </summary>
public static class CsvFileReader
{
    /// <summary>
    /// Reads a CSV file and parses the data.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="UserDetails"/> objects.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the CSV file.</exception>
    public static async Task<List<UserDetails>> ReadCsvFileAsync(string csvFilePath)
    {
        using StringReader csvFileReader = new(await File.ReadAllTextAsync(csvFilePath));

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
                ConsoleUtils.WriteError($"{csvLine} [Invalid]");
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
    public static async Task<List<UserDetails>> GetDeltaAsync(List<UserDetails> currentList, List<UserDetails> lastRunList, int maxTasks = 5)
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
            var deltaTask = Task.Run(async () =>
            {
                await semaphoreSlim.WaitAsync();

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
            });

            deltaTasks.Add(deltaTask);
        }

        await Task.WhenAll(deltaTasks);

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