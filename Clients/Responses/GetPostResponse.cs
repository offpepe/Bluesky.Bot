using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Responses;

public record GetPostResponse(Post[] posts);