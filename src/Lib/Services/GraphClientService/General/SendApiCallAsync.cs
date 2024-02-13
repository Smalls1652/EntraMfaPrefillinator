using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;

namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <summary>
    /// Send an API call to the Graph API.
    /// </summary>
    /// <param name="endpoint">The endpoint to call.</param>
    /// <param name="httpMethod">The HTTP method to use.</param>
    /// <param name="body">Contents to use in the body of the API call.</param>
    /// <returns>If any, the response from the Graph API in string form.</returns>
    private async Task<string?> SendApiCallAsync(string endpoint, HttpMethod httpMethod, string? body)
    {
        await ConnectAsync();

        using HttpClient graphClient = _httpClientFactory.CreateClient("GraphApiClient");

        string? content = null;
        bool isFinished = false;

        while (!isFinished)
        {
            using HttpRequestMessage request = new(
                method: httpMethod,
                requestUri: endpoint
            );

            request.Headers.Authorization = new AuthenticationHeaderValue(
                scheme: "Bearer",
                parameter: _authToken!.AccessToken
            );

            if (body is not null)
            {
                request.Content = new StringContent(
                    content: body,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json"
                );
            }

            using HttpResponseMessage response = await graphClient.SendAsync(request);

            switch (response.StatusCode)
            {
                case HttpStatusCode.TooManyRequests:
                    RetryConditionHeaderValue retryAfter = response.Headers.RetryAfter!;

                    TimeSpan retryBuffer = retryAfter.Delta!.Value.Add(TimeSpan.FromSeconds(15));

                    _logger.LogWarning("[{endpoint}] Currently rate limited by the Graph API. Retrying in {RetryBuffer}s.", endpoint, retryBuffer.TotalSeconds);

                    await Task.Delay(retryBuffer);
                    break;

                default:
                    content = await response.Content.ReadAsStringAsync();
                    isFinished = true;
                    break;
            }
        }

        return content;
    }

    /// <summary>
    /// Send an API call to the Graph API.
    /// </summary>
    /// <param name="endpoint">The endpoint to call.</param>
    /// <param name="httpMethod">The HTTP method to use.</param>
    /// <returns>If any, the response from the Graph API in string form.</returns>
    private async Task<string?> SendApiCallAsync(string endpoint, HttpMethod httpMethod) => await SendApiCallAsync(endpoint, httpMethod, null);
}