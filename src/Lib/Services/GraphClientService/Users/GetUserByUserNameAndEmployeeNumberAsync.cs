using System.Diagnostics;
using System.Text;
using System.Web;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <inheritdoc />
    public async Task<User?> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber) => await GetUserByUserNameAndEmployeeNumberAsync(userName, employeeNumber, null);
    public async Task<User?> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber, string? parentActivityId)
    {
        using var activity = _activitySource.StartActivity(
            name: "GetUserByUserNameAndEmployeeNumberAsync",
            kind: ActivityKind.Client,
            tags: new ActivityTagsCollection
            {
                { "userName", userName },
                { "employeeNumber", employeeNumber }
            },
            parentId: parentActivityId
        );

        if (userName is null && employeeNumber is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Both userName and employeeNumber cannot be null.");
            throw new ArgumentNullException(nameof(userName), "Both userName and employeeNumber cannot be null.");
        }

        StringBuilder apiFilterBuilder = new();

        if (userName is not null)
        {
            apiFilterBuilder.Append($"startsWith(userPrincipalName, '{userName}@')");
        }

        if (employeeNumber is not null)
        {
            if (apiFilterBuilder.Length > 0)
            {
                apiFilterBuilder.Append(" or ");
            }

            apiFilterBuilder.Append($"employeeId eq '{employeeNumber}'");
        }

        string apiFilter = HttpUtility.UrlEncode(apiFilterBuilder.ToString());
        string apiEndpoint = $"users?$filter={apiFilter}&$select=id,userPrincipalName,displayName,onPremisesSamAccountName,employeeId";

        string apiResultString = await SendApiCallAsync(
            endpoint: apiEndpoint,
            httpMethod: HttpMethod.Get
        ) ?? throw new Exception("API result was null.");
        
        User? user;
        GraphCollection<User> userCollection;
        try
        {
            userCollection = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphCollectionUser
            )!;

            if (userCollection.Value is null)
            {
                return null;
            }

            user = Array.Find(userCollection.Value, item => item.OnPremisesSamAccountName == userName || item.EmployeeId == employeeNumber) ?? throw new Exception("User not found.");

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