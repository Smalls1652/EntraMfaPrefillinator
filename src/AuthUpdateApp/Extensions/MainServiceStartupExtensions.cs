using EntraMfaPrefillinator.AuthUpdateApp.Services;

namespace EntraMfaPrefillinator.AuthUpdateApp.Extensions;

/// <summary>
/// Extension methods for configuring the <see cref="MainService"/> in the
/// dependency injection container.
/// </summary>
internal static class MainServiceStartupExtensions
{
    /// <summary>
    /// Adds the <see cref="MainService"/> to the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMainService(this IServiceCollection services) => AddMainService(services, _ => { });
    
    /// <summary>
    /// Adds the <see cref="MainService"/> to the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A delegate to configure the <see cref="MainServiceOptions"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddMainService(this IServiceCollection services, Action<MainServiceOptions> configure)
    {
        services.Configure(configure);

        services.AddHostedService<MainService>();

        return services;
    }
}
