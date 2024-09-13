namespace bsky.bot.Clients.Models;

public sealed record GeminiInstruction(
    string role, 
    GeminiRequestPart[] parts);