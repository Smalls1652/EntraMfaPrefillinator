using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

public interface ICsvFileReaderService
{
    Task<List<UserDetails>> ReadCsvFileAsync(string csvFilePath);
    Task<List<UserDetails>> GetDeltaAsync(List<UserDetails> currentList, CancellationToken cancellationToken = default);
}