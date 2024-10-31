using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public record GetPostResponse(Post[] posts);