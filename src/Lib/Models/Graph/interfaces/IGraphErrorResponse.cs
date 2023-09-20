namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Interface for Graph API error responses.
/// </summary>
public interface IGraphErrorResponse
{
    /// <summary>
    /// The error data returned by the Graph API.
    /// </summary>
    GraphError? Error { get; set; }
}