using System.Text.Json;
using System.Text.Json.Serialization;

public static class JsonOptionsFactory
{
    public static JsonSerializerOptions CamelCaseEnum()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
