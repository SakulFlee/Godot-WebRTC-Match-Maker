using System.Text.Json;
using System.Text.Json.Serialization;

public class GamePacket
{
    public GamePacketType Type;
    public string Inner;

    /// <summary>
    /// Deserializes (/Parse) from JSON
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>An instance of this GamePacket class</returns>
    public static GamePacket FromJSON(string json)
    {
        return JsonSerializer.Deserialize<GamePacket>(json, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new JsonStringEnumConverter(),
            },
        });
    }

    /// <summary>
    /// Serializes this instance to JSON
    /// </summary>
    /// <returns>This GamePacket as JSON</returns>
    public string ToJSON()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = {
                new JsonStringEnumConverter(),
            },
        });
    }

    public static GamePacket MakeAddPlayerPacket(string peerUUID)
    {
        return new GamePacket
        {
            Type = GamePacketType.AddPlayer,
            Inner = peerUUID,
        };
    }
}