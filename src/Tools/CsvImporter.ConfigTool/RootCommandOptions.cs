using System.CommandLine;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool;

/// <summary>
/// Options for the root command.
/// </summary>
public sealed class RootCommandOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootCommandOptions"/> class.
    /// </summary>
    /// <param name="parseResult">The parse result from the command line.</param>
    public RootCommandOptions(ParseResult parseResult)
    {
        DatabasePath = ParseDatabasePathOption(parseResult);
    }

    /// <summary>
    /// The file path to the SQLite database.
    /// </summary>
    public string DatabasePath { get; set; }

    /// <summary>
    /// Parses the '--database-path' option.
    /// </summary>
    /// <param name="parseResult">The parse result.</param>
    /// <returns>The value of the '--database-path' option.</returns>
    /// <exception cref="InvalidOperationException">Thrown when '--database-path' is null.</exception>
    private static string ParseDatabasePathOption(ParseResult parseResult)
    {
        return parseResult.GetValue<string>("--database-path") ?? throw new InvalidOperationException("--database-path is required.");
    }
}
