using System.Security.Cryptography.X509Certificates;

namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Holds the credentials for authenticating with the Microsoft Graph API.
/// </summary>
public class GraphClientCredential : IGraphClientCredential
{
    public GraphClientCredential(GraphClientCredentialType credentialType)
    {
        CredentialType = credentialType;
    }

    public GraphClientCredential(GraphClientCredentialType credentialType, string clientSecret)
    {
        CredentialType = credentialType;
        ClientSecret = clientSecret;
    }

    public GraphClientCredential(GraphClientCredentialType credentialType, X509Certificate2 clientCertificate)
    {
        CredentialType = credentialType;
        ClientCertificate = clientCertificate;
    }

    /// <inheritdoc />
    public GraphClientCredentialType CredentialType { get; }

    /// <inheritdoc />
    public string? ClientSecret { get; }

    /// <inheritdoc />
    public X509Certificate2? ClientCertificate { get; }
}
