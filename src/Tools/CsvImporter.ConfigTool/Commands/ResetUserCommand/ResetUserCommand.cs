using System.CommandLine;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

/// <summary>
/// Command to reset the state for a user, so they can be reprocessed.
/// </summary>
public sealed class ResetUserCommand : CliCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetUserCommand"/> class.
    /// </summary>
    public ResetUserCommand() : base("reset-user")
    {
        Description = "Reset the state for a user, so they can be reprocessed.";

        Options.Add(
            new CliOption<string[]>("--employee-number")
            {
                Description = "The employee number of the user.",
                Required = false
            }
        );

        Options.Add(
            new CliOption<string[]>("--username")
            {
                Description = "The username of the user.",
                Required = false
            }
        );

        Action = new ResetUserCommandAction();
    }
}
