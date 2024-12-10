using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MatchMakerResponse))]
[JsonSerializable(typeof(MatchMakerRequest))]
[JsonSerializable(typeof(MatchMakerUpdate))]
[JsonSerializable(typeof(Packet))]
public partial class JsonSourceGenContext : JsonSerializerContext
{
}
