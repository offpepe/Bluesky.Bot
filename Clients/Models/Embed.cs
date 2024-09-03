using bsky.bot.Clients.Enums;

namespace bsky.bot.Clients.Models;

public static class Embed
{
    public static Dictionary<string, object> GetEmbed(string uri, string title, string description, string blob,
        string mimeType, int size) => new()
    {
        { "$type", EmbedTypes.External },
        {
            "external", new External(
                uri, 
                title,
                description,
                GetThumbValue(blob, mimeType, size)
            )
        }
    };

    private static Dictionary<string, object> GetThumbValue(string blob, string mimeType, int size) => new()
    {
        { "$type", "blob" },
        { "ref", new Dictionary<string, string>() { { "$link", blob } } },
        { "mimeType", mimeType },
        { "size", size }
    };
}

public readonly record struct External(
    string uri,
    string title,
    string description,
    Dictionary<string, object> thumb);