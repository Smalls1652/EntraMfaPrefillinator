using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

/// <summary>
/// Extension methods for configuring the <see cref="GraphClientService"/> in the
/// dependency injection container.
/// </summary>
public static class GraphClientServiceExtensions
{
    /// <summary>
    /// Adds the <see cref="GraphClientService"/> to the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="graphClientConfig">The config for the <see cref="GraphClientService"/>.</param>
    /// <param name="disableAuthUpdate">Whether to disable auth updates from being made.</param>
    /// <returns>The modified <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGraphClientService(this IServiceCollection services, GraphClientConfig graphClientConfig, bool disableAuthUpdate = false)
    {
        services.AddSingleton<IGraphClientService, GraphClientService>(service => new GraphClientService(graphClientConfig, disableAuthUpdate));
        return services;
    }
}