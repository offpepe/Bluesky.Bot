using System.Text.Json.Serialization;

namespace bsky.bot.Clients.Models;

public sealed class RecordEmbed
{
    [JsonPropertyName("$type")] public string type { get; init; } = null!;
    public EmbedImage[] images { get; init; } = null!;
    public Subject? record { get; init; }
    public RecordEmbed? media { get; init; }
    public External? external { get; init; }
    public string? text { get; init; }
}
public readonly record struct AspectRatio(int heigth, int width);
public readonly record struct External()
{
    public string description { get; init; }
    public object? thumb { get; init; }
    public string title { get; init; }
    public string uri { get; init; }
}
public readonly record struct ExternalPost()
{
    public string description { get; init; }
    public string? thumb { get; init; }
    public string title { get; init; }
    public string uri { get; init; }
}
public readonly struct PostEmbed
{
    [JsonPropertyName("$type")] public string type { get; init; }
    public PostEmbedRecord? record { get; init; }
    public RecordEmbed? media { get; init; }
    public External? external { get; init; }
    public EmbedImage[] images { get; init; }
}
public sealed class PostEmbedRecord
{
    [JsonPropertyName("$type")] public string type { get; init; } = null!;
    public string? uri { get; init; }
    public string? cid { get; init; }
    public Author? author { get; init; }
    public Record? value { get; init; }
    public PostEmbedRecord? record { get; init; }
}
public readonly record struct EmbedImage(
    string? thumb,
    string? fullsize,
    string alt,
    AspectRatio aspectRatio,
    Image image
);