using System.Text.RegularExpressions;
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
}