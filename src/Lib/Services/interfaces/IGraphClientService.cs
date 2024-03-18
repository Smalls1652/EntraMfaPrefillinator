using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public interface IGraphClientService
{
    Task<User?> GetUserAsync(string userId);
    Task<User?> GetUserAsync(string userId, string? parentActivityId);

    Task<User?> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber);
    Task<User?> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber, string? parentActivityId);

    Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber);
    Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber, string? parentActivityId);

    Task<PhoneAuthenticationMethod> AddOfficePhoneAuthenticationMethodAsync(string userId, string phoneNumber);
    Task<PhoneAuthenticationMethod> AddOfficePhoneAuthenticationMethodAsync(string userId, string phoneNumber, string? parentActivityId);

    Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress);
    Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress, string? parentActivityId);

    Task<PhoneAuthenticationMethod[]?> GetPhoneAuthenticationMethodsAsync(string userId);
    Task<PhoneAuthenticationMethod[]?> GetPhoneAuthenticationMethodsAsync(string userId, string? parentActivityId);

    Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId);
    Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId, string? parentActivityId);
}