using System.Text.Json;
using System.Text.Json.Serialization;

namespace Code_Kata.Parsers;

public static class JsonParser
{
    public static T Parse<T>(string json)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
        return JsonSerializer.Deserialize<T>(json, options);
    }

    public static string ToJson<T>(T entity)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(entity, options);
    }
}