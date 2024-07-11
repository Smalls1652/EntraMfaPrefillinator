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
public sealed class ResetUserCommandAction : AsynchronousCliAction
{
    /// <inheritdoc />
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        using ILoggerFactory loggerFactory = LoggerUtilities.CreateLoggerFactory();
        ILogger logger = loggerFactory.CreateLogger("Reset User Command");

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
            logger.LogError(ex, "Failed to parse command options.");
            return 1;
        }

        // Connect to the database and find the user.
        using UserDetailsDbContext dbContext = new(
            options: new DbContextOptionsBuilder<UserDetailsDbContext>()
                .UseSqlite($"Data Source={rootOptions.DatabasePath}")
                .Options
        );

        List<UserDetails> userDetails = [];

        if (options.EmployeeNumber is not null && options.EmployeeNumber.Length > 0)
        {
            foreach (string employeeNumber in options.EmployeeNumber)
            {
                UserDetails? userItem = await dbContext.UserDetails
                    .SingleOrDefaultAsync(item => item.EmployeeNumber == employeeNumber, cancellationToken);

                if (userItem is not null)
                {
                    userDetails.Add(userItem);
                }
                else
                {
                    logger.LogWarning("No user found with employee number '{EmployeeNumber}'.", employeeNumber);
                }
            }
        }
        else if (options.Username is not null && options.Username.Length > 0)
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
                    logger.LogWarning("No user found with username '{Username}'.", username);
                }
            }
        }
        else
        {
            logger.LogError("Either an employee number or username must be provided.");
            return 1;
        }

        if (userDetails.Count == 0)
        {
            logger.LogError("No users found.");
            return 1;
        }

        foreach (UserDetails user in userDetails)
        {
            logger.LogInformation("Resetting state for '{UserName}' ({EmployeeNumber}).", user.UserName, user.EmployeeNumber);
            dbContext.UserDetails.Remove(user);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return 0;
    }
}
