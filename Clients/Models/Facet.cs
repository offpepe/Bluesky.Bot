using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Models;

public sealed class Facet
{
    [JsonPropertyName("$type")] public string type { get; } = "app.bsky.richtext.facet";
    public FacetIndex index { get; set; }
    public Feature[] features { get; set; }
}

public readonly record struct FacetIndex(int byteStart, int byteEnd);

public sealed class Feature
{
    [JsonPropertyName("$type")] public string type { get; set; } = null!;
    public string? tag { get; set; }
    public string? uri { get; set; }
    public string? did { get; set; }
}