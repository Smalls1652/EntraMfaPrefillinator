namespace EntraMfaPrefillinator.Tools.CsvImporter.Services;

/// <summary>
/// Options for configuring the <see cref="ICsvImporterSqliteService"/>.
/// </summary>
public class CsvImporterSqliteServiceOptions
{
    /// <summary>
    /// The path to the SQLite database.
    /// </summary>
    public string DbPath { get; set; } = null!;
}