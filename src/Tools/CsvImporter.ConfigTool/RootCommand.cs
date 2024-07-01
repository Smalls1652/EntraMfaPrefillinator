using System.CommandLine;

using EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Commands;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool;

/// <summary>
/// The base root command for the CsvImporter Config Tool.
/// </summary>
public sealed class RootCommand : CliRootCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RootCommand"/> class.
    /// </summary>
    public RootCommand() : base("EntraMfaPrefillinator CSV Importer Config Tool")
    {
        string configDirPath = Path.Combine(
            path1: Environment.IsPrivilegedProcess
                ? Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)
                : Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            path2: "EntraMfaPrefillinator",
            path3: "CsvImporter"
        );

        string defaultDbPath = Path.Combine(
            path1: configDirPath,
            path2: "CsvImporter.sqlite"
        );

        Options.Add(
            new CliOption<string>("--database-path")
            {
                Description = "The file path to the SQLite database.",
                Required = true,
                Recursive = true,
                DefaultValueFactory = (_) => defaultDbPath
            }
        );

        Add(new ResetUserCommand());
        Add(new GetUserCommand());
    }
}
