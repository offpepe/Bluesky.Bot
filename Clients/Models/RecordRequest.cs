using System.Text.Json.Serialization;
using bsky.bot.Clients.Requests;

namespace bsky.bot.Clients.Models;

public class RecordRequest
{
    [JsonPropertyName("$type")] public string type { get; init; }
    public string createdAt { get; init; }
    public Subject? subject { get; init; }
    public Reply? reply { get; init; }
    public string? text { get; init; }
}