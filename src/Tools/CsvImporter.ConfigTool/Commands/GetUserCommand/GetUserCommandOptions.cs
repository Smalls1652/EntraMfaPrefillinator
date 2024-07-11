using System.CommandLine;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Options for the 'get-user' command.
/// </summary>
public sealed class GetUserCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserCommandOptions"/> class.
    /// </summary>
    /// <param name="parseResult">The parse result of the command.</param>
    public GetUserCommandOptions(ParseResult parseResult)
    {
        EmployeeNumber = ParseEmployeeNumberOption(parseResult);
        Username = ParseUsernameOption(parseResult);
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
    /// Parses the employee number option from the parse result.
    /// </summary>
    /// <param name="parseResult">The parse result of the command.</param>
    /// <returns>The employee number option value.</returns>
    private static string? ParseEmployeeNumberOption(ParseResult parseResult)
    {
        return parseResult.GetValue<string>("--employee-number");
    }

    /// <summary>
    /// Parses the username option from the parse result.
    /// </summary>
    /// <param name="parseResult">The parse result of the command.</param>
    /// <returns>The username option value.</returns>
    private static string? ParseUsernameOption(ParseResult parseResult)
    {
        return parseResult.GetValue<string>("--username");
    }
}
