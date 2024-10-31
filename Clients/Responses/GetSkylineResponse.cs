using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public readonly record struct GetSkylineResponse(SkylineObject[] feed);
    

public readonly record struct SkylineObject(Post post, PostReply? reply);
public readonly record struct PostReply(Post root, Post parent);
