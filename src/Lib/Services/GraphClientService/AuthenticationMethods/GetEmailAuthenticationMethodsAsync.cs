using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    public async Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId) => await GetEmailAuthenticationMethodsAsync(userId, null);
    public async Task<EmailAuthenticationMethod[]?> GetEmailAuthenticationMethodsAsync(string userId, string? parentActivityId)
    {
        using var activity = _activitySource.StartActivity(
            name: "GetEmailAuthenticationMethodsAsync",
            kind: ActivityKind.Client,
            tags: new ActivityTagsCollection
            {
                { "userId", userId }
            },
            parentId: parentActivityId
        );

        string apiEndpoint = $"users/{userId}/authentication/emailMethods";

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Get
        ) ?? throw new Exception("API result was null.");

        GraphCollection<EmailAuthenticationMethod> emailAuthMethods;
        try
        {
            emailAuthMethods = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphCollectionEmailAuthenticationMethod
            )!;

            return emailAuthMethods.Value;
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