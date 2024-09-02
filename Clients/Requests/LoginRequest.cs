namespace bsky.bot.Clients.Requests;

public readonly record struct LoginRequest(string identifier, string password);