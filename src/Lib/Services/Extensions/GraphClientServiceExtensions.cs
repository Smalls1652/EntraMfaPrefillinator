using EntraMfaPrefillinator.Lib.Models.Graph;
using Microsoft.Extensions.DependencyInjection;

namespace EntraMfaPrefillinator.Lib.Services.Extensions;

/// <summary>
/// Extension methods for configuring the <see cref="GraphClientService"/> in the
/// dependency injection container.
/// </summary>
public static class GraphClientServiceExtensions
{
    public static IServiceCollection AddGraphClientService(this IServiceCollection services, Action<GraphClientServiceOptions> configure)
    {
        services.Configure(configure);

        services.AddHttpClient(
            name: "GraphApiClient",
            configureClient: clientOptions =>
            {
                clientOptions.BaseAddress = new Uri("https://graph.microsoft.com/beta/");
                clientOptions.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
            }
        );

        services.AddSingleton<IGraphClientService, GraphClientService>();
        return services;
    }
}