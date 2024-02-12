using System.Text;
using System.Web;
using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <inheritdoc />
    public async Task<User> GetUserByUserNameAndEmployeeNumberAsync(string? userName, string? employeeNumber)
    {
        if (userName is null && employeeNumber is null)
        {
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
        
        User user;
        GraphCollection<User> userCollection;
        try
        {
            userCollection = JsonSerializer.Deserialize(
                json: apiResultString,
                jsonTypeInfo: GraphJsonContext.Default.GraphCollectionUser
            )!;

            if (userCollection.Value is null)
            {
                throw new Exception("User collection value is null.");
            }

            user = Array.Find(userCollection.Value, item => item.OnPremisesSamAccountName == userName || item.EmployeeId == employeeNumber) ?? throw new Exception("User not found.");

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