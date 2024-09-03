namespace bsky.bot.Clients.Responses;

public readonly record struct GetHrefMetadataResponse(
        string? error,
        string likely_type,
        string title,
        string description,
        string image
    );