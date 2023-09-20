using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public interface IGraphClientService
{
    HttpClient GraphClient { get; }

    Task<User> GetUserAsync(string userId);
    Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber);
    Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress);
    Task<PhoneAuthenticationMethod[]?> GetPhoneAuthenticationMethodsAsync(string userId);
    Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId);
}