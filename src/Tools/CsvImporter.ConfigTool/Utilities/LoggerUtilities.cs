using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Tools.CsvImporter.ConfigTool.Utilities;

/// <summary>
/// Utility methods for creating loggers.
/// </summary>
public static class LoggerUtilities
{
    /// <summary>
    /// Creates an <see cref="ILoggerFactory"/> with a simple console logger.
    /// </summary>
    /// <returns>The logger factory.</returns>
    public static ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(configure =>
        {
            configure.AddSimpleConsole(options =>
            {
                options.IncludeScopes = false;
                options.SingleLine = false;
            });

            configure.AddFilter("Default", LogLevel.Information);
        });
    }
}
