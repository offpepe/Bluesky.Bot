using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Objects;

public class RefKeyObj
{
    [JsonPropertyName("$link")] public string link { get; set; } = null!;
}