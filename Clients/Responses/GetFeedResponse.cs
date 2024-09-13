using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Responses;

public readonly record struct GetFeedResponse(
    FeedObject[] feed,
    string cursor
);

public struct FeedObject(Post post, string? feedContext);