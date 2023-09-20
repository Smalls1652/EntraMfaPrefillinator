namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Holds the configuration for the Microsoft Graph API client.
/// </summary>
public class GraphClientConfig : IGraphClientConfig
{
    /// <inheritdoc />
    public string ClientId { get; set; } = null!;

    /// <inheritdoc />
    public string TenantId { get; set; } = null!;

    /// <inheritdoc />
    public string[] ApiScopes { get; set; } = null!;

    /// <inheritdoc />
    public IGraphClientCredential Credential { get; set; } = null!;
}