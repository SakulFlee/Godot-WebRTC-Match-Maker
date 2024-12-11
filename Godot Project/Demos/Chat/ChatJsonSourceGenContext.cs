using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    IncludeFields = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(ChatMessagePacket))]
public partial class ChatJsonSourceGenContext : JsonSerializerContext
{
}
