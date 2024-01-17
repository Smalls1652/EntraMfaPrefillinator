using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using EntraMfaPrefillinator.Tools.CsvImporter.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Utilities;

public static class AzureKeyVaultUtils
{
    public static async Task<string> GetSecretFromKeyVaultAsync(Uri vaultUri, string secretName)
    {
        ManagedIdentityTokenCredential managedIdentityTokenCredential;
        try
        {
            managedIdentityTokenCredential = new();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting access token for Azure: {ex.Message}", ex);
        }

        SecretClient secretClient = new(
            vaultUri: vaultUri,
            credential: managedIdentityTokenCredential
        );

        KeyVaultSecret secret = await secretClient.GetSecretAsync(secretName);

        return secret.Value;
    }
}