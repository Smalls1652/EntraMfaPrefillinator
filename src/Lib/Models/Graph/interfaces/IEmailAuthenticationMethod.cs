namespace EntraMfaPrefillinator.Lib.Models.Graph;

public interface IEmailAuthenticationMethod
{
    string? Id { get; set; }
    string EmailAddress { get; set; }
}