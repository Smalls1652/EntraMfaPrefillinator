namespace EntraMfaPrefillinator.Lib.Models.Graph;

public interface IPhoneAuthenticationMethod
{
    string? Id { get; set; }
    string PhoneNumber { get; set; }
    string PhoneType { get; set; }
    string? SmsSignInState { get; set; }
}