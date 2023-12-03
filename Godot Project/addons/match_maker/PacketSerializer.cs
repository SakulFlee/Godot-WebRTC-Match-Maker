using System.Text.Json;
using System.Text.Json.Serialization;

public class PacketSerializer<T>
{
    /// <summary>
    /// Deserializes (/Parse) from JSON
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>An instance of this T class</returns>
    public static T FromJSON(string json)
    {
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions()
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
    /// <returns>This T as JSON</returns>
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