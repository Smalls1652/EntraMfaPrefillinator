using System.Text.Json.Serialization;

namespace EntraMfaPrefillinator.Tools.CsvImporter.Models;

public sealed class QueueMessageTimeToLiveConfig
{
    [JsonPropertyName("firstRun")]
    public TimeSpan FirstRun { get; set; } = TimeSpan.FromHours(12);

    [JsonPropertyName("deltaRuns")]
    public TimeSpan DeltaRuns { get; set; } = TimeSpan.FromMinutes(30);
}