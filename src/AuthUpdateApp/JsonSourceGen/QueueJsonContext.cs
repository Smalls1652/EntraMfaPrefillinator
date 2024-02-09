using EntraMfaPrefillinator.Lib.Models;

namespace EntraMfaPrefillinator.AuthUpdateApp;

/// <summary>
/// Source generated JSON context for queue message types.
/// </summary>
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(UserAuthUpdateQueueItem))]
[JsonSerializable(typeof(UserAuthUpdateQueueItem[]))]
internal partial class QueueJsonContext : JsonSerializerContext
{
}