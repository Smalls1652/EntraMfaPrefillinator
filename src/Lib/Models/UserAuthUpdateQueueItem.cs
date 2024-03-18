namespace EntraMfaPrefillinator.Lib.Models;

public class UserAuthUpdateQueueItem : IUserAuthUpdateQueueItem
{
    [JsonPropertyName("employeeId")]
    public string? EmployeeId { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("userPrincipalName")]
    public string? UserPrincipalName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("homePhone")]
    public string? HomePhone { get; set; }
}