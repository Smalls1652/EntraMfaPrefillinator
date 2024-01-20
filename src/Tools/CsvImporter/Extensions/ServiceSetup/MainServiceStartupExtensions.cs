using EntraMfaPrefillinator.Tools.CsvImporter.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Extensions.ServiceSetup;

public static class MainServiceStartupExtensions
{
    public static IServiceCollection AddMainService(this IServiceCollection services, Action<MainServiceOptions> options)
    {
        services.Configure(options);

        services.AddHostedService<MainService>();

        return services;
    }
}
