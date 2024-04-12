using System.Data.Common;
using EntraMfaPrefillinator.Lib.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

/// <summary>
/// Service for interacting with the SQLite database.
/// </summary>
public class CsvImporterSqliteService : ICsvImporterSqliteService, IDisposable
{
    private readonly ILogger _logger;
    private readonly CsvImporterSqliteServiceOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvImporterSqliteService"/> class.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="options">Options for configuring the <see cref="CsvImporterSqliteService"/>.</param>
    public CsvImporterSqliteService(ILogger<CsvImporterSqliteService> logger, IOptions<CsvImporterSqliteServiceOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private SqliteConnection? _connection;

    /// <summary>
    /// Initializes the database if it does not already exist.
    /// </summary>
    /// <returns></returns>
    public async Task InitializeDatabaseAsync()
    {
        if (File.Exists(_options.DbPath))
        {
            return;
        }

        _logger.LogInformation("Initializing database...");

        await using SqliteConnection connection = new($"Data Source={_options.DbPath}");

        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();

        string tableCreateCommand = @"CREATE TABLE IF NOT EXISTS UserDetails (
    EmployeeNumber TEXT PRIMARY KEY,
    UserName TEXT,
    SecondaryEmail TEXT NULL,
    PhoneNumber TEXT NULL,
    HomePhoneNumber TEXT NULL
);";

        command.CommandText = tableCreateCommand;

        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("Database initialized.");
    }

    /// <summary>
    /// Opens a connection to the database.
    /// </summary>
    /// <returns></returns>
    public async Task OpenAsync()
    {
        if (_connection is not null)
        {
            return;
        }

        _connection = new($"Data Source={_options.DbPath}");

        await _connection.OpenAsync();
    }

    /// <summary>
    /// Creates a command for inserting a <see cref="UserDetails"/> object into the database.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDetails"/> object to insert.</param>
    /// <returns>A <see cref="SqliteCommand"/> for inserting a <see cref="UserDetails"/> object into the database.</returns>
    public SqliteCommand CreateInsertUserDetailsCommand(UserDetails userDetails)
    {
        SqliteCommand command = _connection!.CreateCommand();

        string insertCommand = @"INSERT INTO UserDetails (EmployeeNumber, UserName, SecondaryEmail, PhoneNumber, HomePhoneNumber)
VALUES (@EmployeeNumber, @UserName, @SecondaryEmail, @PhoneNumber, @HomePhoneNumber);";

        command.CommandText = insertCommand;
        command.Parameters.AddWithValue("@EmployeeNumber", userDetails.EmployeeNumber);
        command.Parameters.AddWithValue("@UserName", userDetails.UserName);
        command.Parameters.AddWithValue("@SecondaryEmail", userDetails.SecondaryEmail ?? string.Empty);
        command.Parameters.AddWithValue("@PhoneNumber", userDetails.PhoneNumber ?? string.Empty);
        command.Parameters.AddWithValue("@HomePhoneNumber", userDetails.HomePhoneNumber ?? string.Empty);

        return command;
    }

    /// <summary>
    /// Creates a command for updating a <see cref="UserDetails"/> object in the database.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDetails"/> object to update.</param>
    /// <returns>A <see cref="SqliteCommand"/> for updating a <see cref="UserDetails"/> object in the database.</returns>
    public SqliteCommand CreateUpdateUserDetailsCommand(UserDetails userDetails)
    {
        SqliteCommand command = _connection!.CreateCommand();

        string updateCommand = @"UPDATE UserDetails SET UserName = @UserName, SecondaryEmail = @SecondaryEmail, PhoneNumber = @PhoneNumber, HomePhoneNumber = @HomePhoneNumber WHERE EmployeeNumber = @EmployeeNumber;";

        command.CommandText = updateCommand;
        command.Parameters.AddWithValue("@EmployeeNumber", userDetails.EmployeeNumber);
        command.Parameters.AddWithValue("@UserName", userDetails.UserName);
        command.Parameters.AddWithValue("@SecondaryEmail", userDetails.SecondaryEmail ?? string.Empty);
        command.Parameters.AddWithValue("@PhoneNumber", userDetails.PhoneNumber ?? string.Empty);
        command.Parameters.AddWithValue("@HomePhoneNumber", userDetails.HomePhoneNumber ?? string.Empty);

        return command;
    }

    /// <summary>
    /// Inserts a <see cref="UserDetails"/> object into the database.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDetails"/> object to insert.</param>
    /// <returns></returns>
    public async Task InsertUserDetailsAsync(UserDetails userDetails)
    {
        try
        {
            _logger.LogInformation("Inserting user details for '{UserName}' [{EmployeeNumber}]...", userDetails.UserName, userDetails.EmployeeNumber);

            if (_connection is null)
            {
                await OpenAsync();
            }

            await using SqliteCommand command = _connection!.CreateCommand();

            string insertCommand = @"INSERT INTO UserDetails (EmployeeNumber, UserName, SecondaryEmail, PhoneNumber, HomePhoneNumber)
VALUES (@EmployeeNumber, @UserName, @SecondaryEmail, @PhoneNumber, @HomePhoneNumber);";

            command.CommandText = insertCommand;
            command.Parameters.AddWithValue("@EmployeeNumber", userDetails.EmployeeNumber);
            command.Parameters.AddWithValue("@UserName", userDetails.UserName);
            command.Parameters.AddWithValue("@SecondaryEmail", userDetails.SecondaryEmail ?? string.Empty);
            command.Parameters.AddWithValue("@PhoneNumber", userDetails.PhoneNumber ?? string.Empty);
            command.Parameters.AddWithValue("@HomePhoneNumber", userDetails.HomePhoneNumber ?? string.Empty);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting user details for '{UserName}' [{EmployeeNumber}].", userDetails.UserName, userDetails.EmployeeNumber);
            throw;
        }
    }

    /// <summary>
    /// Runs a batch of database updates.
    /// </summary>
    /// <param name="commands">A collection of <see cref="SqliteCommand"/> to run.</param>
    /// <returns></returns>
    public async Task RunDbUpdatesAsync(IEnumerable<SqliteCommand> commands)
    {
        try
        {
            if (_connection is null)
            {
                await OpenAsync();
            }

            await using SqliteTransaction transaction = _connection!.BeginTransaction();

            foreach (SqliteCommand command in commands)
            {
                command.Transaction = transaction;

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            foreach (SqliteCommand command in commands)
            {
                command.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting user details batch.");
            throw;
        }
    }

    /// <summary>
    /// Gets a <see cref="UserDetails"/> object from the database.
    /// </summary>
    /// <param name="userDetails">The <see cref="UserDetails"/> object to get.</param>
    /// <returns>A <see cref="UserDetails"/> object from the database.</returns>
    public async Task<UserDetails?> GetUserDetailsAsync(UserDetails userDetails) => await GetUserDetailsAsync(userDetails.EmployeeNumber!);

    /// <summary>
    /// Gets a <see cref="UserDetails"/> object from the database.
    /// </summary>
    /// <param name="employeeNumber">The employee number of the <see cref="UserDetails"/> object to get.</param>
    /// <returns>A <see cref="UserDetails"/> object from the database.</returns>
    public async Task<UserDetails?> GetUserDetailsAsync(string employeeNumber)
    {
        try
        {
            if (_connection is null)
            {
                await OpenAsync();
            }

            await using SqliteCommand command = _connection!.CreateCommand();

            string selectCommand = @"SELECT EmployeeNumber, UserName, SecondaryEmail, PhoneNumber, HomePhoneNumber FROM UserDetails WHERE EmployeeNumber = @EmployeeNumber;";

            command.CommandText = selectCommand;
            command.Parameters.AddWithValue("@EmployeeNumber", employeeNumber);

            await using DbDataReader reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                return null;
            }

            await reader.ReadAsync();

            string? retrievedSecondaryEmail = reader.GetString(2);
            string? retrievedPhoneNumber = reader.GetString(3);

            // If the secondary email or phone number is null or empty, set it to null.
            // This is to prevent null checks from being broken.
            if (string.IsNullOrEmpty(retrievedSecondaryEmail))
            {
                retrievedSecondaryEmail = null;
            }

            if (string.IsNullOrEmpty(retrievedPhoneNumber))
            {
                retrievedPhoneNumber = null;
            }

            UserDetails userDetails = new()
            {
                EmployeeNumber = reader.GetString(0),
                UserName = reader.GetString(1),
                SecondaryEmail = retrievedSecondaryEmail,
                PhoneNumber = retrievedPhoneNumber
            };

            return userDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details for '{EmployeeNumber}'.", employeeNumber);
            throw;
        }
    }

    /// <summary>
    /// Gets all <see cref="UserDetails"/> objects from the database.
    /// </summary>
    /// <returns>A collection of <see cref="UserDetails"/> objects from the database.</returns>
    public async Task<List<UserDetails>?> GetAllUserDetailsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all user details...");

            if (_connection is null)
            {
                await OpenAsync();
            }

            await using SqliteCommand command = _connection!.CreateCommand();

            string selectCommand = @"SELECT EmployeeNumber, UserName, SecondaryEmail, PhoneNumber, HomePhoneNumber FROM UserDetails;";

            command.CommandText = selectCommand;

            await using DbDataReader reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                return null;
            }

            List<UserDetails> userDetailsList = [];

            while (await reader.ReadAsync())
            {
                UserDetails userDetails = new()
                {
                    EmployeeNumber = reader.GetString(0),
                    UserName = reader.GetString(1),
                    SecondaryEmail = reader.GetString(2),
                    PhoneNumber = reader.GetString(3),
                    HomePhoneNumber = reader.GetString(4)
                };

                userDetailsList.Add(userDetails);
            }

            return userDetailsList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all user details.");
            throw;
        }
    }

    /// <summary>
    /// Gets the number of <see cref="UserDetails"/> objects in the database.
    /// </summary>
    /// <returns>The number of <see cref="UserDetails"/> objects in the database.</returns>
    public async Task<int> GetUserDetailsCountAsync()
    {
        try
        {
            if (_connection is null)
            {
                await OpenAsync();
            }

            await using SqliteCommand command = _connection!.CreateCommand();

            string selectCommand = @"SELECT COUNT(*) FROM UserDetails;";

            command.CommandText = selectCommand;

            await using DbDataReader reader = await command.ExecuteReaderAsync();

            if (!reader.HasRows)
            {
                return 0;
            }

            await reader.ReadAsync();

            int count = reader.GetInt32(0);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user details count.");
            throw;
        }
    }

    /// <summary>
    /// Closes the connection to the database.
    /// </summary>
    /// <returns></returns>
    public async Task CloseAsync()
    {
        if (_connection is null)
        {
            return;
        }

        await _connection.CloseAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _connection?.Dispose();

        GC.SuppressFinalize(this);
    }
}