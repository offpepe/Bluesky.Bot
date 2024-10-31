namespace bsky.bot.Clients.Objects;

public sealed record GeminiInstruction(
    string role, 
    GeminiRequestPart[] parts);