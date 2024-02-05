using EntraMfaPrefillinator.AuthUpdateApp.Models;

namespace EntraMfaPrefillinator.AuthUpdateApp;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(AuthUpdateAppConfig))]
internal partial class CoreJsonContext : JsonSerializerContext
{}