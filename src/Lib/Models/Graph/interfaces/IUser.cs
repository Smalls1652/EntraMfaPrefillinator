namespace EntraMfaPrefillinator.Lib.Models.Graph;

public interface IUser
{
    string Id { get; set; }
    string DisplayName { get; set; }
    string UserPrincipalName { get; set; }
    string OnPremisesSamAccountName { get; set; }
    string? EmployeeId { get; set; }
}