namespace EntraMfaPrefillinator.Lib.Models.Graph;

/// <summary>
/// Houses data for an error returned by the Graph API.
/// </summary>
public class GraphError : IGraphError
{
    /// <inheritdoc />
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <inheritdoc />
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}