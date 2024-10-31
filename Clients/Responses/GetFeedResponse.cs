using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public readonly record struct GetFeedResponse(
    FeedObject[] Feed,
    string Cursor
);

public struct FeedObject(Post post, string? feedContext);
