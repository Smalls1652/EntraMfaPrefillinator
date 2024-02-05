namespace EntraMfaPrefillinator.AuthUpdateApp.Models;

public class AuthUpdateAppConfig
{
    [JsonPropertyName("CLIENT_ID")]
    public string ClientId { get; set; } = null!;

    [JsonPropertyName("TENANT_ID")]
    public string TenantId { get; set; } = null!;

    [JsonPropertyName("CLIENT_SECRET")]
    public string ClientSecret { get; set; } = null!;

    [JsonPropertyName("ENABLE_DRY_RUN")]
    public bool EnableDryRun { get; set; }
}