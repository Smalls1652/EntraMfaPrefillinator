using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;

using EntraMfaPrefillinator.Lib.Models.Graph;
using Microsoft.Identity.Client.AppConfig;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <summary>
    /// Get an authentication token to connect to the Graph API.
    /// </summary>
    /// <returns><see cref="AuthenticationResult" /></returns>
    private async Task<AuthenticationResult> GetAuthTokenAsync()
    {
        AuthenticationResult authToken;

        try
        {
            if (_graphClientCredentialType == GraphClientCredentialType.ClientSecret)
            {
                authToken = await GetConfidentialClientAuthTokenAsync();
            }
            else if (_graphClientCredentialType == GraphClientCredentialType.SystemManagedIdentity)
            {
                authToken = await GetSystemManagedIdentityAuthTokenAsync();
            }
            else
            {
                throw new InvalidOperationException("Unsupported credential type.");
            }
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "Failed to get an authentication token for the Graph API.");
            throw;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "The credential type supplied is not supported.");
            throw;
        }

        return authToken;
    }

    private async Task<AuthenticationResult> GetConfidentialClientAuthTokenAsync()
    {
        AuthenticationResult authToken;

        try
        {
            IConfidentialClientApplication confidentialClient = ConfidentialClientApplicationBuilder
                .Create(_options.ClientId)
                .WithClientSecret(_options.Credential.ClientSecret)
                .WithTenantId(_options.TenantId)
                .Build();

            authToken = await confidentialClient
                .AcquireTokenForClient(_apiScopes)
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "Failed to get an authentication token for the Graph API.");
            throw;
        }

        return authToken;
    }

    private async Task<AuthenticationResult> GetSystemManagedIdentityAuthTokenAsync()
    {
        AuthenticationResult authToken;

        try
        {
            IManagedIdentityApplication managedIdentity = ManagedIdentityApplicationBuilder
                .Create(ManagedIdentityId.SystemAssigned)
                .Build();

            authToken = await managedIdentity
                .AcquireTokenForManagedIdentity(
                    resource: "https://graph.microsoft.com"
                )
                .ExecuteAsync()
                .ConfigureAwait(false);
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "Failed to get an authentication token for the Graph API.");
            throw;
        }

        return authToken;
    }
}
