namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Houses data for an error response returned by the Graph API.
/// </summary>
public class GraphErrorResponse : IGraphErrorResponse
{
    /// <inheritdoc />
    [JsonPropertyName("error")]
    public GraphError? Error { get; set; }
}