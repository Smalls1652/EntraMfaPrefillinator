namespace EntraMfaPrefillinator.Lib.Models;

public interface IUserAuthUpdateQueueItem
{
    string UserPrincipalName { get; set; }
    string? EmailAddress { get; set; }
    string? PhoneNumber { get; set; }
}