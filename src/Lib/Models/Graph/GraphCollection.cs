namespace EntraMfaPrefillinator.Lib.Models.Graph;

public class GraphCollection<T> : IGraphCollection<T>
{
    [JsonPropertyName("value")]
    public T[]? Value { get; set; }
}