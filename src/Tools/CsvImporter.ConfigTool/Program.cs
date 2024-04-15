using System.CommandLine;

using EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool;

CliConfiguration cliConfig = new(
    rootCommand: new RootCommand()
);

return await cliConfig.InvokeAsync(args);
