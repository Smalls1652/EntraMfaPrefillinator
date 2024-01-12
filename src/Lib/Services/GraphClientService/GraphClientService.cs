using System.Text.RegularExpressions;
using EntraMfaPrefillinator.Lib.Models.Graph;
using Microsoft.Identity.Client;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService : IGraphClientService
{
    private readonly HttpClient _graphClient;
    private readonly IEnumerable<string> _apiScopes;
    private readonly bool _disableUpdateMethods;
    private readonly IConfidentialClientApplication _confidentialClientApplication;

    public GraphClientService(GraphClientConfig graphClientConfig)
    {
        _apiScopes = graphClientConfig.ApiScopes;

        _confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(graphClientConfig.ClientId)
            .WithClientSecret(graphClientConfig.Credential.ClientSecret)
            .WithTenantId(graphClientConfig.TenantId)
            .Build();

        _graphClient = new()
        {
            BaseAddress = new Uri("https://graph.microsoft.com/beta/")
        };
        
        _graphClient.DefaultRequestHeaders.Add("ConsistencyLevel", "eventual");
    }

    public GraphClientService(GraphClientConfig graphClientConfig, bool disableUpdateMethods) : this(graphClientConfig)
    {
        _disableUpdateMethods = disableUpdateMethods;
    }

    public HttpClient GraphClient => _graphClient;

    private bool _isConnected => _authToken is not null;
    private AuthenticationResult? _authToken;

    [GeneratedRegex("^https:\\/\\/graph.microsoft.com\\/(?'version'v1\\.0|beta)\\/(?'endpoint'.+?)$")]
    private partial Regex _nextLinkRegex();
}