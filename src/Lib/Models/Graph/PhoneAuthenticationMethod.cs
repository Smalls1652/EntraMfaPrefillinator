namespace EntraMfaPrefillinator.Lib.Models.Graph;

public class PhoneAuthenticationMethod : IPhoneAuthenticationMethod
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = null!;

    [JsonPropertyName("phoneType")]
    public string PhoneType { get; set; } = null!;

    [JsonPropertyName("smsSignInState")]
    public string? SmsSignInState { get; set; }
}