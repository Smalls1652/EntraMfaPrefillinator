namespace EntraMfaPrefillinator.Lib.Models.Graph;

public class EmailAuthenticationMethod : IEmailAuthenticationMethod
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("emailAddress")]
    public string EmailAddress { get; set; } = null!;
}