using System.Text.Json;

public interface ChatPacketSerializer
{
    /// <summary>
    /// Deserializes (/Parse) from JSON
    /// </summary>
    /// <param name="json">The JSON string to parse</param>
    /// <returns>An instance of this T class</returns>
    public static T FromJSON<T>(string json) where T : class
    {
        return JsonSerializer.Deserialize(json, typeof(T), ChatJsonSourceGenContext.Default) as T;
    }

    /// <summary>
    /// Serializes this instance to JSON
    /// </summary>
    /// <returns>This T as JSON</returns>
    public static string ToJSON(object o)
    {
        return JsonSerializer.Serialize(o, o.GetType(), ChatJsonSourceGenContext.Default);
    }
}
