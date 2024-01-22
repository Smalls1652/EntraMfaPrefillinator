namespace EntraMfaPrefillinator.Tools.CsvImporter.Logging;

/// <summary>
/// Configuration for the <see cref="FileLogger"/>.
/// </summary>
public sealed class FileLoggerConfiguration
{
    /// <summary>
    /// The event ID to use for the log.
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// The file path to use for the log.
    /// </summary>
    public string? FilePath { get; set; }
}
