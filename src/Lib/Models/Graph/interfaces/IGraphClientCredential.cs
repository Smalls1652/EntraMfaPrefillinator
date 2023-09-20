using System.Security.Cryptography.X509Certificates;

namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Interface for holding credentials for authenticating with the Microsoft Graph API.
/// </summary>
public interface IGraphClientCredential
{
    /// <summary>
    /// The type of the credential.
    /// </summary>
    GraphClientCredentialType CredentialType { get; }

    /// <summary>
    /// The client secret for the app.
    /// </summary>
    string? ClientSecret { get; }

    /// <summary>
    /// The certificate for the app.
    /// </summary>
    X509Certificate2? ClientCertificate { get; }
}