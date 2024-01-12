using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber)
    {
        if (_disableUpdateMethods)
        {
            throw new GraphClientDryRunException("AddPhoneAuthenticationMethodAsync() was called, but the service is currently configured to disable update methods.");
        }

        string apiEndpoint = $"users/{userId}/authentication/phoneMethods";

        PhoneAuthenticationMethod newPhoneAuthMethod = new()
        {
            PhoneNumber = phoneNumber,
            PhoneType = "mobile"
        };

        string? apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Post,
            body: JsonSerializer.Serialize(
                value: newPhoneAuthMethod,
                jsonTypeInfo: GraphJsonContext.Default.PhoneAuthenticationMethod
            )
        );

        if (apiResultString is null)
        {
            throw new Exception("API result string is null.");
        }

        PhoneAuthenticationMethod phoneAuthMethod;
        try
        {
            phoneAuthMethod = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.PhoneAuthenticationMethod
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

        return phoneAuthMethod;
    }
}