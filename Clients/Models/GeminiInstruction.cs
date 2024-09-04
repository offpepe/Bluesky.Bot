namespace bsky.bot.Clients.Models;

public readonly record struct GeminiInstruction(
    string role, 
    GeminiRequestPart[] parts);