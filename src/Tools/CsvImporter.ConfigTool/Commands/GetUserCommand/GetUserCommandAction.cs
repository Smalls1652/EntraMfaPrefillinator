using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;

using EntraMfaPrefillinator.Lib.Models;
using EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Utilities;
using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Action for the 'get-user' command.
/// </summary>
public sealed class GetUserCommandAction : AsynchronousCliAction, IDisposable
{
    private bool _disposed;
    
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserCommandAction"/> class.
    /// </summary>
    public GetUserCommandAction()
    {
        _loggerFactory = LoggerUtilities.CreateLoggerFactory();
        _logger = _loggerFactory.CreateLogger("Get User Command");
    }

    /// <inheritdoc />
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        RootCommandOptions rootOptions;
        GetUserCommandOptions options;
        
        try
        {
            rootOptions = new(parseResult);
            options = new(parseResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse command options.");
            return 1;
        }

        if (options.EmployeeNumber is null && options.Username is null)
        {
            _logger.LogError("Either an employee number or username must be provided.");
            return 1;
        }

        using UserDetailsDbContext dbContext = new(
            options: new DbContextOptionsBuilder<UserDetailsDbContext>()
                .UseSqlite($"Data Source={rootOptions.DatabasePath}")
                .Options
        );

        UserDetails? userDetails = (options.EmployeeNumber is not null) switch
        {
            true => await dbContext.UserDetails
                .SingleAsync(item => item.EmployeeNumber == options.EmployeeNumber, cancellationToken),
            _ => await dbContext.UserDetails
                .SingleAsync(item => item.UserName == options.Username, cancellationToken)
        };

        if (userDetails is null)
        {
            _logger.LogWarning("User not found.");
            return 10;
        }

        StringBuilder outputBuilder = new();

        string employeeNumberHeader = "Employee Number";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) employeeNumberSpacing = GetSpacing(employeeNumberHeader, userDetails.EmployeeNumber);

        string usernameHeader = "Username";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) usernameSpacing = GetSpacing(usernameHeader, userDetails.UserName);

        string phoneNumberHeader = "Phone Number";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) phoneNumberSpacing = GetSpacing(phoneNumberHeader, userDetails.PhoneNumber);

        string homePhoneNumberHeader = "Home Phone Number";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) homePhoneNumberSpacing = GetSpacing(homePhoneNumberHeader, userDetails.HomePhoneNumber);

        string emailHeader = "Secondary Email";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) emailSpacing = GetSpacing(emailHeader, userDetails.SecondaryEmail);

        string lastUpdatedHeader = "Last Updated";
        (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) lastUpdatedSpacing = GetSpacing(lastUpdatedHeader, userDetails.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss"));

        outputBuilder
            .Append("|")
            .Append($" {employeeNumberHeader}{new string(' ', employeeNumberSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append($" {usernameHeader}{new string(' ', usernameSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append($" {phoneNumberHeader}{new string(' ', phoneNumberSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append($" {homePhoneNumberHeader}{new string(' ', homePhoneNumberSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append($" {emailHeader}{new string(' ', emailSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append($" {lastUpdatedHeader}{new string(' ', lastUpdatedSpacing.HeaderSpacing)} ")
            .Append("|")
            .Append(Environment.NewLine)
            .Append("|")
            .Append($" {new string('-', employeeNumberSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append($" {new string('-', usernameSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append($" {new string('-', phoneNumberSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append($" {new string('-', homePhoneNumberSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append($" {new string('-', emailSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append($" {new string('-', lastUpdatedSpacing.SeparatorSpacing)} ")
            .Append("|")
            .Append(Environment.NewLine)
            .Append("|")
            .Append($" {userDetails.EmployeeNumber ?? "N/A"}{new string(' ', employeeNumberSpacing.ColumnSpacing)} ")
            .Append("|")
            .Append($" {userDetails.UserName ?? "N/A"}{new string(' ', usernameSpacing.ColumnSpacing)} ")
            .Append("|")
            .Append($" {userDetails.PhoneNumber ?? "N/A"}{new string(' ', phoneNumberSpacing.ColumnSpacing)} ")
            .Append("|")
            .Append($" {userDetails.HomePhoneNumber ?? "N/A"}{new string(' ', homePhoneNumberSpacing.ColumnSpacing)} ")
            .Append("|")    
            .Append($" {userDetails.SecondaryEmail ?? "N/A"}{new string(' ', emailSpacing.ColumnSpacing)} ")
            .Append("|")
            .Append($" {userDetails.LastUpdated?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A"}{new string(' ', lastUpdatedSpacing.ColumnSpacing)} ")
            .Append("|");

        Console.WriteLine(outputBuilder.ToString());

        return 0;
    }

    /// <summary>
    /// Get the spacing for the header and input values.
    /// </summary>
    /// <param name="headerValue">The header value.</param>
    /// <param name="inputValue">The input value.</param>
    /// <returns>A tuple containing the header spacing, column spacing, and separator spacing.</returns>
    private (int HeaderSpacing, int ColumnSpacing, int SeparatorSpacing) GetSpacing(string headerValue, string? inputValue)
    {
        int headerValueLength = headerValue.Length;
        int inputValueLength = inputValue?.Length ?? "N/A".Length;

        bool isInputLonger = inputValueLength > headerValueLength;

        int headerSpacing = isInputLonger
            ? inputValueLength - headerValueLength
            : 0;

        int columnSpacing = isInputLonger
            ? 0
            : headerValueLength - inputValueLength;

        int separatorSpacing = columnSpacing < headerSpacing
            ? inputValueLength
            : headerValueLength;

        return (headerSpacing, columnSpacing, separatorSpacing);
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
