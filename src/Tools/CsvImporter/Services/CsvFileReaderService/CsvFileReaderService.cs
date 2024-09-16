using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Lib.Services;
using EntraMfaPrefillinator.Lib.Utilities;
using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

/// <summary>
/// Houses methods for reading CSV files.
/// </summary>
public sealed class CsvFileReaderService : ICsvFileReaderService
{
    private readonly ILogger _logger;
    private readonly IGraphClientService _graphClientService;
    private readonly IDbContextFactory<UserDetailsDbContext> _dbContextFactory;

    public CsvFileReaderService(ILoggerFactory loggerFactory, IGraphClientService graphClientService, IDbContextFactory<UserDetailsDbContext> dbContextFactory)
    {
        _logger = loggerFactory.CreateLogger("CsvFileReaderService");
        _graphClientService = graphClientService;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Reads a CSV file and parses the data.
    /// </summary>
    /// <param name="csvFilePath">The path to the CSV file.</param>
    /// <returns>A <see cref="List{T}"/> of <see cref="UserDetails"/> objects.</returns>
    /// <exception cref="Exception">Thrown when there is an error reading the CSV file.</exception>
    public async Task<List<UserDetails>> ReadCsvFileAsync(string csvFilePath, CancellationToken cancellationToken = default)
    {
        using StreamReader csvFileReader = new(File.OpenRead(csvFilePath));

        List<UserDetails> userDetailsList = [];

        string? csvLine;
        int currentLine = 0;
        while ((csvLine = await csvFileReader.ReadLineAsync(cancellationToken)) is not null)
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
                UserDetails userDetails = new(csvLine);

                try
                {
                    await userDetails.GetEntraUserInfoAsync(_graphClientService, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error retrieving user info for {userDetails}: {message}", userDetails.UserName, ex.Message);
                }

                userDetailsList.Add(userDetails);
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
        using UserDetailsDbContext dbContext = _dbContextFactory.CreateDbContext();
        
        List<UserDetails> deltaList = [];

        foreach (var userDetailsItem in currentList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            UserDetails? lastRunUserDetailsItem = await dbContext.UserDetails
                .FirstOrDefaultAsync(item => item == userDetailsItem, cancellationToken: cancellationToken);

            // If the user was not found in the last run CSV file, add the user to the delta list.
            if (lastRunUserDetailsItem is null)
            {
                userDetailsItem.IsInLastRun = false;
                deltaList.Add(userDetailsItem);
                continue;
            }

            // If the user was found in the last run CSV file and the email or phone number has changed,
            // add the user to the delta list.
            if (lastRunUserDetailsItem.PhoneNumber != userDetailsItem.PhoneNumber
                || lastRunUserDetailsItem.SecondaryEmail != userDetailsItem.SecondaryEmail
                || lastRunUserDetailsItem.HomePhoneNumber != userDetailsItem.HomePhoneNumber)
            {
                userDetailsItem.IsInLastRun = true;
                deltaList.Add(userDetailsItem);
                continue;
            }

            // If the user was found in the last run CSV file and the user's object ID
            // in EntraID has changed, add the user to the delta list.
            if (lastRunUserDetailsItem.EntraUserId != userDetailsItem.EntraUserId)
            {
                userDetailsItem.IsInLastRun = true;
                userDetailsItem.UserWasRecreated = true;
                deltaList.Add(userDetailsItem);
                continue;
            }
        }

        return deltaList;
    }
}
