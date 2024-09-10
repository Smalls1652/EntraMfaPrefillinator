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
    private readonly GraphClientServiceOptions _options;
    private readonly GraphClientCredentialType _graphClientCredentialType;
    private readonly ActivitySource _activitySource = new("EntraMfaPrefillinator.Lib.Services.GraphClientService");

    public GraphClientService(ILogger<GraphClientService> logger, IHttpClientFactory httpClientFactory, IOptions<GraphClientServiceOptions> options)
    {
        _logger = logger;

        _httpClientFactory = httpClientFactory;

        _options = options.Value;
        _apiScopes = _options.ApiScopes;
        _graphClientCredentialType = _options.Credential.CredentialType;

        if (_graphClientCredentialType != GraphClientCredentialType.ClientSecret && _graphClientCredentialType != GraphClientCredentialType.SystemManagedIdentity)
        {
            throw new InvalidOperationException($"Invalid GraphClientCredentialType: {_graphClientCredentialType}");
        }

        _disableUpdateMethods = _options.DisableUpdateMethods;
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
