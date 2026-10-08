using System.Text.Json;

namespace Rasa.Api
{
    /// <summary>Shared JSON serialization for API payloads that are not written by ServerStatus.</summary>
    public static class ApiJson
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
    }
}
