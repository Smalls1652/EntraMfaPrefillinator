using EntraMfaPrefillinator.Lib.Models.Graph;

namespace EntraMfaPrefillinator.Lib;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(User[]))]
[JsonSerializable(typeof(EmailAuthenticationMethod))]
[JsonSerializable(typeof(EmailAuthenticationMethod[]))]
[JsonSerializable(typeof(GraphCollection<EmailAuthenticationMethod>))]
[JsonSerializable(typeof(PhoneAuthenticationMethod))]
[JsonSerializable(typeof(PhoneAuthenticationMethod[]))]
[JsonSerializable(typeof(GraphCollection<PhoneAuthenticationMethod>))]
[JsonSerializable(typeof(GraphErrorResponse))]
[JsonSerializable(typeof(GraphError))]
internal partial class GraphJsonContext : JsonSerializerContext
{
}