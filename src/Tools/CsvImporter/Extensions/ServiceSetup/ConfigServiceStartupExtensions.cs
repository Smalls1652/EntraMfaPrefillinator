using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Tools.CsvImporter;

public static class ConfigServiceStartupExtensions
{
    public static IServiceCollection AddConfigService(this IServiceCollection services, Action<ConfigServiceOptions> options)
    {
        services.Configure(options);

        services.AddSingleton<IConfigService, ConfigService>();

        return services;
    }
}
