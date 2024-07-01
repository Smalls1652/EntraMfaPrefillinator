using System.CommandLine;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Command to get the current state of a user that has been processed.
/// </summary>
public sealed class GetUserCommand : CliCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserCommand"/> class.
    /// </summary>
    public GetUserCommand() : base("get-user")
    {
        Description = "Get the current state of a user that has been processed.";

        Options
            .AddEmployeeNumberOption()
            .AddUsernameOption();

        Action = new GetUserCommandAction();
    }
}

file static class GetUserCommandExtensions
{
    /// <summary>
    /// Adds the employee number option to the command.
    /// </summary>
    /// <param name="options">The options to add to.</param>
    /// <returns>The options with the employee number option added.</returns>
    public static IList<CliOption> AddEmployeeNumberOption(this IList<CliOption> options)
    {
        CliOption<string> employeeNumberOption = new("--employee-number")
        {
            Description = "The employee number of a user.",
            Required = false
        };

        options.Add(employeeNumberOption);

        return options;
    }

    /// <summary>
    /// Adds the username option to the command.
    /// </summary>
    /// <param name="options">The options to add to.</param>
    /// <returns>The options with the username option added.</returns>
    public static IList<CliOption> AddUsernameOption(this IList<CliOption> options)
    {
        CliOption<string> usernameOption = new("--username")
        {
            Description = "The username of a user.",
            Required = false
        };

        options.Add(usernameOption);

        return options;
    }
}
