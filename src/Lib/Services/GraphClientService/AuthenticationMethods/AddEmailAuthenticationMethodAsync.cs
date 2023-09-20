using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress)
    {
        string apiEndpoint = $"users/{userId}/authentication/emailMethods";

        EmailAuthenticationMethod newEmailAuthMethod = new()
        {
            EmailAddress = emailAddress
        };

        string? apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Post,
            body: JsonSerializer.Serialize(
                value: newEmailAuthMethod,
                jsonTypeInfo: GraphJsonContext.Default.EmailAuthenticationMethod
            )
        );

        if (apiResultString is null)
        {
            throw new Exception("API result string is null.");
        }

        EmailAuthenticationMethod emailAuthMethod;
        try
        {
            emailAuthMethod = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.EmailAuthenticationMethod
            )!;
        }
        catch
        {
            GraphErrorResponse? errorResponse = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphErrorResponse
            );

            throw new Exception(errorResponse!.Error!.Message);
        }

        return emailAuthMethod;
    }
}