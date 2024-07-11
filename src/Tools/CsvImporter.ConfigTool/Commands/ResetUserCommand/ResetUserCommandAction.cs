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

    private UserDetailsDbContext? _dbContext;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetUserCommandAction"/> class.
    /// </summary>
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
        _dbContext = new(
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
                UserDetails? userItem = await _dbContext.UserDetails
                    .SingleOrDefaultAsync(item => item.EmployeeNumber == employeeNumber, cancellationToken);

                if (userItem is not null)
                {
                    try
                    {
                        ResetUserState(userItem);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(ex, "A critical error occurred while resetting the user.");
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while resetting the user.");
                    }
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
                UserDetails? userItem = await _dbContext.UserDetails
                    .SingleOrDefaultAsync(item => item.UserName == username, cancellationToken);

                if (userItem is not null)
                {
                    try
                    {
                        ResetUserState(userItem);
                    }
                    catch (InvalidOperationException ex)
                    {
                        _logger.LogError(ex, "A critical error occurred while resetting the user.");
                        return 1;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while resetting the user.");
                    }
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return 0;
    }

    /// <summary>
    /// Remove the user from the database to reset their state.
    /// </summary>
    /// <param name="user">The user to reset.</param>
    private void ResetUserState(UserDetails user)
    {
        if (_dbContext is null)
        {
            throw new InvalidOperationException("Database context not initialized.");
        }

        _logger.LogInformation("Resetting state for '{UserName}' ({EmployeeNumber}).", user.UserName, user.EmployeeNumber);
        _dbContext.UserDetails.Remove(user);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _loggerFactory.Dispose();
        _dbContext?.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
