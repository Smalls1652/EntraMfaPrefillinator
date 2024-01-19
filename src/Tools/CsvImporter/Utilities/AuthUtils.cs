using Azure.Core;
using Azure.Identity;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

public static class AuthUtils
{
    public static string GetStorageConnectionString()
    {
        string? connectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING") ?? throw new NullReferenceException("STORAGE_CONNECTION_STRING environment variable is null.");

        return connectionString;
    }

    public static TokenCredential CreateTokenCredential(string? queueUri)
    {
        string queueUriString = queueUri ?? Environment.GetEnvironmentVariable("QUEUE_URI") ?? throw new NullReferenceException("QUEUE_URI environment variable not set or missing from config file.");

        try
        {
            return new ChainedTokenCredential(
                new AzureCliCredential(),
                new AzurePowerShellCredential(),
                new ManagedIdentityCredential()
            );
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating token credential for '{queueUriString}': {ex.Message}", ex);
        }
    }
}
