using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <inheritdoc />
    public async Task<User> GetUserAsync(string userId)
    {
        string apiEndpoint = $"users/{userId}?$select=id,userPrincipalName,displayName";

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Get
        ) ?? throw new Exception("API result was null.");

        User user;
        try
        {
            user = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.User
            )!;

            if (string.IsNullOrEmpty(user.Id))
            {
                throw new Exception("User ID is null or empty.");
            }
        }
        catch
        {
            GraphErrorResponse? errorResponse = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphErrorResponse
            );

            throw new Exception(errorResponse!.Error!.Message);
        }

        return user;
    }
}