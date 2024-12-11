using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    IncludeFields = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(GamePacket))]
[JsonSerializable(typeof(GamePacketInput))]
[JsonSerializable(typeof(GamePacketPlayer))]
[JsonSerializable(typeof(GamePacketType))]
public partial class GameJsonSourceGenContext : JsonSerializerContext
{
}
