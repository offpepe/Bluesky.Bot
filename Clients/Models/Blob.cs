using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Models;

public class Blob
{
    [JsonPropertyName("$type")]
    public string Type { get; set; }

    [JsonPropertyName("ref")]
    public RefKeyObj Reference { get; set; }

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; }
    
    [JsonPropertyName("size")]
    public int Size { get; set; }
}