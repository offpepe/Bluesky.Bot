using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Models;

public class RefKeyObj
{
    [JsonPropertyName("$link")] public string link { get; set; } = null!;
}