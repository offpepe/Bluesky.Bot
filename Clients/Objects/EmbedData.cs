namespace bsky.bot.Clients.Objects;

public readonly record struct EmbedData(
    string uri,
    string title,
    string description,
    string blob,
    string mimeType,
    int size);