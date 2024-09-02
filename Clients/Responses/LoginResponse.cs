namespace bsky.bot.Clients.Responses;

public readonly record struct LoginResponse(string accessJwt, string did);