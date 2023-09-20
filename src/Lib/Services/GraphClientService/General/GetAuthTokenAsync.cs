using Microsoft.Identity.Client;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <summary>
    /// Get an authentication token to connect to the Graph API.
    /// </summary>
    /// <returns><see cref="AuthenticationResult" /></returns>
    private async Task<AuthenticationResult> GetAuthTokenAsync()
    {
        AuthenticationResult? authToken = await _confidentialClientApplication
            .AcquireTokenForClient(_apiScopes)
            .ExecuteAsync();

        return authToken;
    }
}