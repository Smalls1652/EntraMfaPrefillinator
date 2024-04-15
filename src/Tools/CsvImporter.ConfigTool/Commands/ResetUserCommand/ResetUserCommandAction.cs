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

        UserDetails? user = options.EmployeeNumber is not null
            ? await dbContext.UserDetails
                .FirstOrDefaultAsync(item => item.EmployeeNumber == options.EmployeeNumber, cancellationToken)
            : await dbContext.UserDetails
                .FirstOrDefaultAsync(item => item.UserName == options.Username, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("User not found.");
            return 1;
        }

        logger.LogInformation("Resetting state for '{userName}' ({employeeNumber}).", user.UserName, user.EmployeeNumber);

        // Remove the user from the database and save the changes.
        dbContext.UserDetails.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("User state reset.");

        return 0;
    }
}
