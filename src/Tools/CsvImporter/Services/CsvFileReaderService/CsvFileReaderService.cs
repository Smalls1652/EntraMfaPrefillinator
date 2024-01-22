using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.Utilities;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

/// <summary>
/// Houses methods for reading CSV files.
/// </summary>
public sealed class CsvFileReaderService : ICsvFileReaderService
{
    private readonly ILogger _logger;
    private readonly ICsvImporterSqliteService _dbService;

    public CsvFileReaderService(ILoggerFactory loggerFactory, ICsvImporterSqliteService dbService)
    {
        _logger = loggerFactory.CreateLogger("CsvFileReaderService");
        _dbService = dbService;
    }

    /// <summary>
    /// Reads a CSV file and parses the data.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="UserDetails"/> objects.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the CSV file.</exception>
    public async Task<List<UserDetails>> ReadCsvFileAsync(string csvFilePath)
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
                _logger.LogWarning("Invalid CSV line: {csvLine}", csvLine);
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
    public async Task<List<UserDetails>> GetDeltaAsync(List<UserDetails> currentList, CancellationToken cancellationToken = default)
    {
        List<UserDetails> deltaList = [];

        foreach (var userDetailsItem in currentList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            UserDetails? lastRunUserDetailsItem = await _dbService.GetUserDetailsAsync(userDetailsItem);

            if (lastRunUserDetailsItem is null)
            {
                userDetailsItem.IsInLastRun = false;
                deltaList.Add(userDetailsItem);
                continue;
            }

            // If the user was found in the last run CSV file and the email or phone number has changed,
            // add the user to the delta list.
            if (lastRunUserDetailsItem.PhoneNumber != userDetailsItem.PhoneNumber || lastRunUserDetailsItem.SecondaryEmail != userDetailsItem.SecondaryEmail)
            {
                userDetailsItem.IsInLastRun = true;
                deltaList.Add(userDetailsItem);
            }
        }

        return deltaList;
    }
}