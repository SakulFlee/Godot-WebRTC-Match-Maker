using System.Text.Json;
using System.Text.Json.Serialization;

public class GamePacket
{
    public GamePacketType Type;
    public string InnerJSON;

    /// <summary>
    /// Deserializes (/Parse) from JSON
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>An instance of this Packet class</returns>
    public static Packet FromJSON(string json)
    {
        return JsonSerializer.Deserialize<Packet>(json, new JsonSerializerOptions()
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
    /// <returns>This Packet as JSON</returns>
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
}