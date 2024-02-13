using System.Diagnostics;
using System.Text.RegularExpressions;
using EntraMfaPrefillinator.Lib.Models.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService : IGraphClientService, IDisposable
{
    private bool _disposed;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEnumerable<string> _apiScopes;
    private readonly bool _disableUpdateMethods;
    private readonly IConfidentialClientApplication _confidentialClientApplication;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.Lib.Services.GraphClientService");

    public GraphClientService(ILogger<GraphClientService> logger, IHttpClientFactory httpClientFactory, IOptions<GraphClientServiceOptions> options)
    {
        _logger = logger;

        _httpClientFactory = httpClientFactory;

        var graphClientConfig = options.Value;
        _apiScopes = graphClientConfig.ApiScopes;

        _confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(graphClientConfig.ClientId)
            .WithClientSecret(graphClientConfig.Credential.ClientSecret)
            .WithTenantId(graphClientConfig.TenantId)
            .Build();

        _disableUpdateMethods = graphClientConfig.DisableUpdateMethods;
    }

    private bool _isConnected => _authToken is not null;
    private AuthenticationResult? _authToken;

    [GeneratedRegex("^https:\\/\\/graph.microsoft.com\\/(?'version'v1\\.0|beta)\\/(?'endpoint'.+?)$")]
    private partial Regex _nextLinkRegex();

    public void Dispose()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _activitySource.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}