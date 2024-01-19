using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Logging;

internal sealed class FileLoggerConfigurationOptions : IConfigureOptions<FileLoggerConfiguration>
{
    private readonly IConfiguration _configuration;

    [UnsupportedOSPlatform("browser")]
    public FileLoggerConfigurationOptions(ILoggerProviderConfiguration<FileLoggerProvider> providerConfiguration)
    {
        _configuration = providerConfiguration.Configuration;
    }

    public void Configure(FileLoggerConfiguration options) => _configuration.Bind(options);
}
