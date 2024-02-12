namespace EntraMfaPrefillinator.AuthUpdateApp;

/// <summary>
/// Exception thrown when a configuration value is missing or invalid.
/// </summary>
internal sealed class ConfigException : Exception
{
    public ConfigException(string message) : base(message) { }

    public ConfigException(string message, Exception innerException) : base(message, innerException) { }
}