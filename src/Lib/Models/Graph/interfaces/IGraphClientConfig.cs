namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Interface for configuring the Microsoft Graph client.
/// </summary>
public interface IGraphClientConfig
{
    /// <summary>
    /// The client ID of the Azure AD app.
    /// </summary>
    string ClientId { get; set; }
    
    /// <summary>
    /// The tenant ID of the Azure AD app.
    /// </summary>
    string TenantId { get; set; }
    
    /// <summary>
    /// The API scopes to request.
    /// </summary>
    string[] ApiScopes { get; set; }

    /// <summary>
    /// The credential to use for authenticating with the Microsoft Graph API.
    /// </summary>
    IGraphClientCredential Credential { get; set; }

    /// <summary>
    /// Whether to disable updating authentication methods.
    /// </summary>
    bool DisableUpdateMethods { get; set; }
}