using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    IncludeFields = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(MatchMakerResponse))]
[JsonSerializable(typeof(MatchMakerRequest))]
[JsonSerializable(typeof(MatchMakerUpdate))]
[JsonSerializable(typeof(Packet))]
[JsonSerializable(typeof(PacketType))]
public partial class JsonSourceGenContext : JsonSerializerContext
{
}
