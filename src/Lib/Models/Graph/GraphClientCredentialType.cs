namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// The type of credential to use for authenticating an Azure AD app with the Microsoft Graph API.
/// </summary>
public enum GraphClientCredentialType
{
    /// <summary>
    /// The app uses a client secret for authentication.
    /// </summary>
    ClientSecret,

    /// <summary>
    /// The app uses a certificate for authentication.
    /// </summary>
    ClientCertificate
}