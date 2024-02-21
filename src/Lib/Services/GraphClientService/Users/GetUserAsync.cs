using System.Diagnostics;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <inheritdoc />
    public async Task<User?> GetUserAsync(string userId) => await GetUserAsync(userId, null);
    public async Task<User?> GetUserAsync(string userId, string? parentActivityId)
    {
        using var activity = _activitySource.StartActivity(
            name: "GetUserAsync",
            kind: ActivityKind.Client,
            tags: new ActivityTagsCollection
            {
                { "userId", userId }
            },
            parentId: parentActivityId
        );

        string apiEndpoint = $"users/{userId}?$select=id,userPrincipalName,displayName";

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Get
        ) ?? throw new Exception("API result was null.");

        User? user;
        try
        {
            user = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.User
            )!;

            return user;
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