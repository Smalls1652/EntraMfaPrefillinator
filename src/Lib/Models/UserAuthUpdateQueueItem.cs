namespace EntraMfaPrefillinator.Lib.Models;

public class UserAuthUpdateQueueItem : IUserAuthUpdateQueueItem
{
    [JsonPropertyName("userPrincipalName")]
    public string UserPrincipalName { get; set; } = null!;

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
}