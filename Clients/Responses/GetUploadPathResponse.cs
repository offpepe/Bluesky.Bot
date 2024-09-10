using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Responses;

public sealed class GetUploadPathResponse
{
    [JsonPropertyName("x-goog-upload-url")]
    public string path { get; init; } = null!;
}