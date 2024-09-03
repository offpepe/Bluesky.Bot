using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Models;

public class RefKeyObj
{
    [JsonPropertyName("$link")]
    public string Link { get; set; }
}