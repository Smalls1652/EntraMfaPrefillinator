using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<PhoneAuthenticationMethod[]?> GetPhoneAuthenticationMethodsAsync(string userId)
    {
        string apiEndpoint = $"users/{userId}/authentication/phoneMethods";

        string? apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Get
        );

        if (apiResultString is null)
        {
            throw new Exception("API result string is null.");
        }

        GraphCollection<PhoneAuthenticationMethod> phoneAuthMethods;
        try
        {
            phoneAuthMethods = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphCollectionPhoneAuthenticationMethod
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

        return phoneAuthMethods.Value;
    }
}