using System.Text.Json.Serialization;

/// <summary>
/// The type of <see cref="Packet"/>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PacketType>))]
public enum PacketType
{
    MatchMakerRequest,
    MatchMakerResponse,
    MatchMakerUpdate,
    ICECandidate,
    SessionDescription,
}
