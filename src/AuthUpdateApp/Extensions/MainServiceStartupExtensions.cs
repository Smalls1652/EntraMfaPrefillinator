using EntraMfaPrefillinator.AuthUpdateApp.Services;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions;

public static class MainServiceStartupExtensions
{
    public static IServiceCollection AddMainService(this IServiceCollection services) => AddMainService(services, _ => { });

    public static IServiceCollection AddMainService(this IServiceCollection services, Action<MainServiceOptions> configure)
    {
        services.Configure(configure);

        services.AddHostedService<MainService>();

        return services;
    }
}
