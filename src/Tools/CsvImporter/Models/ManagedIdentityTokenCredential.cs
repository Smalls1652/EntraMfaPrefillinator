using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

/// <summary>
/// An <see cref="TokenCredential"/> implementation that uses the system-assigned managed identity of the server.
/// </summary>
public class ManagedIdentityTokenCredential : TokenCredential
{
    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var managedIdentity = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
            .Build();

        AuthenticationResult authResult = await managedIdentity
            .AcquireTokenForManagedIdentity("https://vault.azure.net")
            .ExecuteAsync()
            .ConfigureAwait(false);

        return new(
            accessToken: authResult.AccessToken,
            expiresOn: authResult.ExpiresOn
        );
    }
}