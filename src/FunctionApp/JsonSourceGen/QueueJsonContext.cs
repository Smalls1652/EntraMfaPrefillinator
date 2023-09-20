using EntraMfaPrefillinator.Lib.Models;

namespace EntraMfaPrefillinator.FunctionApp;

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