namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Interface for error data returned by the Graph API
/// </summary>
public interface IGraphError
{
    /// <summary>
    /// Error code returned by the Graph API.
    /// </summary>
    string? Code { get; set; }

    /// <summary>
    /// Error message returned by the Graph API.
    /// </summary>
    string? Message { get; set; }
}