using System.CommandLine;
using System.CommandLine.Invocation;

using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Utilities;
using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Action for the 'reset-user' command.
/// </summary>
public sealed class ResetUserCommandAction : AsynchronousCliAction, IDisposable
{
    private bool _disposed;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    public ResetUserCommandAction()
    {
        _loggerFactory = LoggerUtilities.CreateLoggerFactory();
        _logger = _loggerFactory.CreateLogger("Reset User Command");
    }

    /// <inheritdoc />
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        // Parse the options passed to the command.
        RootCommandOptions rootOptions;
        ResetUserCommandOptions options;
        try
        {
            rootOptions = new RootCommandOptions(parseResult);
            options = new ResetUserCommandOptions(parseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse command options.");
            return 1;
        }

        // Connect to the database and find the user.
        using UserDetailsDbContext dbContext = new(
            options: new DbContextOptionsBuilder<UserDetailsDbContext>()
                .UseSqlite($"Data Source={rootOptions.DatabasePath}")
                .Options
        );

        //Get the user details from the database.
        List<UserDetails> userDetails = [];
        if (options.EmployeeNumber.Length > 0)
        {
            foreach (string employeeNumber in options.EmployeeNumber)
            {
                UserDetails? userItem = await dbContext.UserDetails
                    .SingleOrDefaultAsync(item => item.EmployeeNumber == employeeNumber, cancellationToken);

                if (userItem is not null)
                {
                    _logger.LogInformation("Resetting state for '{UserName}' ({EmployeeNumber}).", userItem.UserName, userItem.EmployeeNumber);
                    userDetails.Add(userItem);
                }
                else
                {
                    _logger.LogWarning("No user found with employee number '{EmployeeNumber}'.", employeeNumber);
                }
            }
        }
        else if (options.Username.Length > 0)
        {
            foreach (string username in options.Username)
            {
                UserDetails? userItem = await dbContext.UserDetails
                    .SingleOrDefaultAsync(item => item.UserName == username, cancellationToken);

                if (userItem is not null)
                {
                    userDetails.Add(userItem);
                }
                else
                {
                   _logger.LogWarning("No user found with username '{Username}'.", username);
                }
            }
        }
        else
        {
            _logger.LogError("Either an employee number or username must be provided.");
            return 1;
        }

        // If no users were found, log an error and return.
        if (userDetails.Count == 0)
        {
            _logger.LogError("No users found.");
            return 1;
        }

        // Reset the state for each user.
        foreach (UserDetails user in userDetails)
        {
            _logger.LogInformation("Resetting state for '{UserName}' ({EmployeeNumber}).", user.UserName, user.EmployeeNumber);
            dbContext.UserDetails.Remove(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _loggerFactory.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
