namespace bsky.bot.Clients.Models;

public readonly record struct EmbedData(
    string uri,
    string title,
    string description,
    string blob,
    string mimeType,
    int size);