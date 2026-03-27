using System.Text.Json;
using System.Text.Json.Serialization;

namespace SentinelKnowledgebase.ImportCLI;

internal static class JsonDefaults
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }
}
