using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Logging;

/// <summary>
/// Configures the <see cref="FileLoggerConfiguration"/> options.
/// </summary>
/// <remarks>
/// This is optimized for Native AOT compilation.
/// </remarks>
internal sealed class FileLoggerConfigurationOptions : IConfigureOptions<FileLoggerConfiguration>
{
    /// <summary>
    /// The configuration instance to bind to.
    /// </summary>
    private readonly IConfiguration _configuration;

    [UnsupportedOSPlatform("browser")]
    public FileLoggerConfigurationOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        _configuration = providerConfiguration.Configuration;
    }

    /// <inheritdoc />
    public void Configure(FileLoggerConfiguration options) => _configuration.Bind(options);
}
