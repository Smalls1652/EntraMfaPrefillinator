using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber) => await AddPhoneAuthenticationMethodAsync(userId, phoneNumber, null);
    public async Task<PhoneAuthenticationMethod> AddPhoneAuthenticationMethodAsync(string userId, string phoneNumber, string? parentActivityId)
    {
        using var activity = _activitySource.StartActivity(
            name: "AddPhoneAuthenticationMethodAsync",
            kind: ActivityKind.Client,
            tags: new ActivityTagsCollection
            {
                { "userId", userId }
            },
            parentId: parentActivityId
        );

        if (_disableUpdateMethods)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "Dry run mode is enabled.");
            throw new GraphClientDryRunException("AddPhoneAuthenticationMethodAsync() was called, but the service is currently configured to disable update methods.");
        }

        string apiEndpoint = $"users/{userId}/authentication/phoneMethods";

        PhoneAuthenticationMethod newPhoneAuthMethod = new()
        {
            PhoneNumber = phoneNumber,
            PhoneType = "mobile"
        };

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Post,
            body: JsonSerializer.Serialize(
                value: newPhoneAuthMethod,
                jsonTypeInfo: GraphJsonContext.Default.PhoneAuthenticationMethod
            )
        ) ?? throw new Exception("API result was null.");

        PhoneAuthenticationMethod phoneAuthMethod;
        try
        {
            phoneAuthMethod = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.PhoneAuthenticationMethod
            )!;

            return phoneAuthMethod;
        }
        catch (ArgumentNullException)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
        catch (JsonException)
        {
            try
            {
                GraphErrorResponse? errorResponse = JsonSerializer.Deserialize(
                    json: apiResultString,
                    jsonTypeInfo: GraphJsonContext.Default.GraphErrorResponse
                );

                activity?.SetStatus(ActivityStatusCode.Error, errorResponse?.Error?.Message ?? "Unknown error.");
                throw new Exception(errorResponse!.Error!.Message);
            }
            catch (JsonException)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw new Exception("An unknown error occurred.");
            }
            throw;
        }
        catch (Exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}