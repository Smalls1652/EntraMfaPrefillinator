using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using Microsoft.Data.Sqlite;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

public interface ICsvImporterSqliteService
{
    Task InitializeDatabaseAsync();
    Task OpenAsync();
    SqliteCommand CreateInsertUserDetailsCommand(UserDetails userDetails);
    SqliteCommand CreateUpdateUserDetailsCommand(UserDetails userDetails);
    Task InsertUserDetailsAsync(UserDetails userDetails);
    Task RunDbUpdatesAsync(IEnumerable<SqliteCommand> commands);
    Task<List<UserDetails>?> GetAllUserDetailsAsync();
    Task<int> GetUserDetailsCountAsync();
    Task<UserDetails?> GetUserDetailsAsync(UserDetails userDetails);
    Task<UserDetails?> GetUserDetailsAsync(string employeeNumber);
    Task CloseAsync();
}