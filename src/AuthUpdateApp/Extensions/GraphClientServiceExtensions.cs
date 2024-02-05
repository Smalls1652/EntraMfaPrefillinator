using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public static class GraphClientServiceExtensions
{
    public static IServiceCollection AddGraphClientService(this IServiceCollection services, GraphClientConfig graphClientConfig, bool disableAuthUpdate = false)
    {
        services.AddSingleton<IGraphClientService, GraphClientService>(service => new GraphClientService(graphClientConfig, disableAuthUpdate));
        return services;
    }
}