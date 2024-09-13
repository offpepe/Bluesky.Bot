using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Requests;

public readonly record struct UploadFileGeminiRequest(RequestFile file);

public readonly struct RequestFile(string fileName)
{
    [JsonPropertyName("display_name")] public string displayName { get; } = fileName;
}
