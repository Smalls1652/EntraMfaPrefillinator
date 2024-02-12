using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public interface IGraphClientService
{
    Task<User> GetUserAsync(string userId);
    Task<User> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber);
    Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber);
    Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress);
    Task<PhoneAuthenticationMethod[]?> GetPhoneAuthenticationMethodsAsync(string userId);
    Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId);
}