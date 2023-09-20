namespace EntraMfaPrefillinator.Lib.Services;

public partial class GraphClientService
{
    /// <summary>
    /// Connects to the Graph API and/or refreshes the authentication token if necessary.
    /// </summary>
    private async Task ConnectAsync()
    {
        // Invert the current value of _isConnected to determine if we need to connect.
        bool needsToConnect = !_isConnected;

        // If we already have an authentication token, check if it's expired.
        // If it is, we need to set the value for 'needsToConnect' to true to get a new token.
        if (_authToken is not null)
        {
            if (DateTimeOffset.Now >= _authToken.ExpiresOn)
            {
                needsToConnect = true;
            }
        }

        // If needed, get a new authentication token to connect
        // to the Graph API.
        if (needsToConnect)
        {
            _authToken = await GetAuthTokenAsync();
        }
    }
}