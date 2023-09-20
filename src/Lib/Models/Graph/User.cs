namespace EntraMfaPrefillinator.Lib.Models.Graph;

public class User : IUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = null!;

    [JsonPropertyName("userPrincipalName")]
    public string UserPrincipalName { get; set; } = null!;
}