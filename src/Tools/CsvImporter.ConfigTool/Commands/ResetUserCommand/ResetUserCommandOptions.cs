using System.CommandLine;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Options for the 'reset-user' command.
/// </summary>
public sealed class ResetUserCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetUserCommandOptions"/> class.
    /// </summary>
    /// <param name="parseResult">The parse result from the command line.</param>
    /// <exception cref="InvalidOperationException">Thrown when '--employee-number' and '--username' are both null.</exception>
    public ResetUserCommandOptions(ParseResult parseResult)
    {
        EmployeeNumber = ParseEmployeeNumberOption(parseResult);
        Username = ParseUsernameOption(parseResult);

        if (EmployeeNumber is null && Username is null || string.IsNullOrEmpty(EmployeeNumber) && string.IsNullOrEmpty(Username))
        {
            throw new InvalidOperationException("Either --employee-number or --username must be specified.");
        }
    }

    /// <summary>
    /// The employee number of the user.
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// The username of the user.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Parses the '--employee-number' option.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <returns>The value of the '--employee-number' option.</returns>
    private static string? ParseEmployeeNumberOption(ParseResult parseResult)
    {
        return parseResult.GetValue<string>("--employee-number");
    }

    /// <summary>
    /// Parses the '--username' option.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <returns>The value of the '--username' option.</returns>
    private static string? ParseUsernameOption(ParseResult parseResult)
    {
        return parseResult.GetValue<string>("--username");
    }
}
