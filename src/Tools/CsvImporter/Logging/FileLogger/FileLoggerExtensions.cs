using System.Runtime.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Logging;

/// <summary>
/// Extension methods to configure the <see cref="FileLogger"/> to an <see cref="ILoggingBuilder"/>.
/// </summary>
[UnsupportedOSPlatform("browser")]
internal static class FileLoggerExtensions
{
    /// <summary>
    /// Adds a <see cref="FileLogger"/> to the <see cref="ILoggingBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to add the <see cref="FileLogger"/> to.</param>
    /// <param name="configure">Configures the <see cref="FileLoggerConfiguration"/>.</param>
    /// <returns>The modified <see cref="ILoggingBuilder"/>.</returns>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, Action<FileLoggerConfiguration> configure)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, FileLoggerProvider>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FileLoggerConfiguration>, FileLoggerConfigurationOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<FileLoggerConfiguration>, LoggerProviderOptionsChangeTokenSource<FileLoggerConfiguration, FileLoggerProvider>>());

        builder.Services.Configure(configure);

        return builder;
    }
}
