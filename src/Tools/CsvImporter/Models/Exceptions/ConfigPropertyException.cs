namespace EntraMfaPrefillinator.Tools.CsvImporter.Models.Exceptions;

internal sealed class ConfigPropertyException : Exception
{
    public ConfigPropertyException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    public ConfigPropertyException(string message, string propertyName, Exception innerException) : base(message, innerException)
    {
        PropertyName = propertyName;
    }

    public string PropertyName { get; set; } = null!;
}
