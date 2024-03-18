namespace EntraMfaPrefillinator.Lib.Models;

public interface IUserAuthUpdateQueueItem
{
    string? EmployeeId { get; set; }
    string? UserName { get; set; }
    string? UserPrincipalName { get; set; }
    string? EmailAddress { get; set; }
    string? PhoneNumber { get; set; }
    string? HomePhone { get; set; }
}