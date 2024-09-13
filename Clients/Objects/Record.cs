using System.Text.Json.Serialization;
using bsky.bot.Clients.Requests;

namespace bsky.bot.Clients.Models;

public readonly struct Record()
{
    [JsonPropertyName("$type")]
    public string type { get; init; }
    public Reply? reply { get; init;}  
    public DateTime createdAt { get; init;}
    public RecordEmbed? embed { get; init;}
    public string[] langs { get; init; } = [];
    public string text { get; init;}
    
}