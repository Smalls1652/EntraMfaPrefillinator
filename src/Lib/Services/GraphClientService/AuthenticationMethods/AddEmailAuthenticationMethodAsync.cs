using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress) => await AddEmailAuthenticationMethodAsync(userId, emailAddress, null);
    public async Task<EmailAuthenticationMethod> AddEmailAuthenticationMethodAsync(string userId, string emailAddress, string? parentActivityId)
    {
        using var activity = _activitySource.StartActivity(
            name: "AddEmailAuthenticationMethodAsync",
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
            throw new GraphClientDryRunException("AddEmailAuthenticationMethodAsync() was called, but the service is currently configured to disable update methods.");
        }

        string apiEndpoint = $"users/{userId}/authentication/emailMethods";

        EmailAuthenticationMethod newEmailAuthMethod = new()
        {
            EmailAddress = emailAddress
        };

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Post,
            body: JsonSerializer.Serialize(
                value: newEmailAuthMethod,
                jsonTypeInfo: GraphJsonContext.Default.EmailAuthenticationMethod
            )
        ) ?? throw new Exception("API result was null.");

        EmailAuthenticationMethod emailAuthMethod;
        try
        {
            emailAuthMethod = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.EmailAuthenticationMethod
            )!;

            return emailAuthMethod;
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
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw new Exception("An unknown error occurred.", ex);
        }
    }
}