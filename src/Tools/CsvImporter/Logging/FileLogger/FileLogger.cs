using System.Text;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Logging;

/// <summary>
/// Custom <see cref="ILogger"/> implementation that writes to a file.
/// </summary>
public class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly Func<FileLoggerConfiguration> _config;

    public FileLogger(string categoryName, Func<FileLoggerConfiguration> config)
    {
        _categoryName = categoryName;
        _config = config;
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => _config().FilePath is not null;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string?> formatter
    )
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;

        //FileLoggerConfiguration config = _config();

        StringBuilder logLineBuilder = new();

        string? message = formatter(state, exception);

        if (message is null)
        {
            return;
        }

        if (!File.Exists(_config().FilePath))
        {
            File.Create(_config().FilePath!).Dispose();
        }

        logLineBuilder.Append(now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
        logLineBuilder.Append(" [");
        logLineBuilder.Append(logLevel.ToString());
        logLineBuilder.Append("] ");
        logLineBuilder.Append(message);
        logLineBuilder.Append(Environment.NewLine);

        File.AppendAllText(_config().FilePath!, logLineBuilder.ToString());
    }

}
