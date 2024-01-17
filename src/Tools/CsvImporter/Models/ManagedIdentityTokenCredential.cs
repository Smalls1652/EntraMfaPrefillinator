using Azure.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

/// <summary>
/// An <see cref="TokenCredential"/> implementation that uses the system-assigned managed identity of the server.
/// </summary>
public class ManagedIdentityTokenCredential : TokenCredential
{
    private readonly string _resourceScope;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedIdentityTokenCredential"/> class.
    /// </summary>
    /// <param name="resourceScope">
    /// <para>
    /// The resource scope to request an access token for.
    /// </para>
    /// <para>
    /// For example, <c>https://management.azure.com</c> for Azure Resource Manager.
    /// </para>
    /// </param>
    public ManagedIdentityTokenCredential(string resourceScope)
    {
        _resourceScope = resourceScope;
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        return GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        var managedIdentity = ManagedIdentityApplicationBuilder.Create(ManagedIdentityId.SystemAssigned)
            .Build();

        AuthenticationResult authResult = await managedIdentity
            .AcquireTokenForManagedIdentity(_resourceScope)
            .ExecuteAsync(cancellationToken)
            .ConfigureAwait(false);

        return new(
            accessToken: authResult.AccessToken,
            expiresOn: authResult.ExpiresOn
        );
    }
}