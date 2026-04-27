using System.Text.Json;
using System.Text.Json.Serialization;

namespace Code_Kata.Parsers;

public static class JsonParser
{
    public static T Parse<T>(string json)
    {
        var options = CreateOptions();
        return JsonSerializer.Deserialize<T>(json, options)
               ?? throw new JsonException($"Could not deserialize JSON into {typeof(T).Name}.");
    }

    public static List<T> ParseCollection<T>(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);

        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            return Parse<List<T>>(json);
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"Expected a JSON array or an object containing '{propertyName}'.");
        }

        var matchingProperty = document.RootElement
            .EnumerateObject()
            .FirstOrDefault(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        if (matchingProperty.Equals(default(JsonProperty)))
        {
            throw new JsonException($"Expected root property '{propertyName}' containing an array.");
        }

        if (matchingProperty.Value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Root property '{propertyName}' must be a JSON array.");
        }

        return JsonSerializer.Deserialize<List<T>>(matchingProperty.Value.GetRawText(), CreateOptions())
               ?? new List<T>();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };
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