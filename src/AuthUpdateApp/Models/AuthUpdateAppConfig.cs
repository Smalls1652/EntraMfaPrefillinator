namespace EntraMfaPrefillinator.AuthUpdateApp.Models;

/// <summary>
/// Config options for the AuthUpdateApp.
/// </summary>
public sealed class AuthUpdateAppConfig
{
    /// <summary>
    /// The Azure AD/Entra ID client application ID.
    /// </summary>
    [JsonPropertyName("CLIENT_ID")]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The Azure AD/Entra ID tenant ID.
    /// </summary>
    [JsonPropertyName("TENANT_ID")]
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// The Azure AD/Entra ID client application secret.
    /// </summary>
    [JsonPropertyName("CLIENT_SECRET")]
    public string ClientSecret { get; set; } = null!;

    /// <summary>
    /// Whether to enable dry run mode.
    /// </summary>
    [JsonPropertyName("ENABLE_DRY_RUN")]
    public bool EnableDryRun { get; set; }
}