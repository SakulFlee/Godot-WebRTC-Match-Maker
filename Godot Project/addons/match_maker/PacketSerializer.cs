using System.Text.Json;
using System.Text.Json.Serialization;

public interface PacketSerializer
{
    /// <summary>
    /// Deserializes (/Parse) from JSON
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>An instance of this T class</returns>
    public static T FromJSON<T>(string json)
    {
        var options = new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(), },
            TypeInfoResolver = JsonSourceGenContext.Default,
        };
        return JsonSerializer.Deserialize<T>(json, options);
    }

    /// <summary>
    /// Serializes this instance to JSON
    /// </summary>
    /// <returns>This T as JSON</returns>
    public static string ToJSON(object o)
    {
        var options = new JsonSerializerOptions()
        {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(), },
            TypeInfoResolver = JsonSourceGenContext.Default,
        };
        return JsonSerializer.Serialize(o, options);
    }
}