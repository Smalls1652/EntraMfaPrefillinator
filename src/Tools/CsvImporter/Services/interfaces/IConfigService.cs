using EntraMfaPrefillinator.Tools.CsvImporter.Models;

namespace EntraMfaPrefillinator.Tools.CsvImporter;

public interface IConfigService
{
    CsvImporterConfig? Config { get; set; }

    Task LoadConfigAsync();
    Task SaveConfigAsync();
    string GetConfigDirPath();
}
