namespace bsky.bot.Clients.Models;

public readonly record struct GenerationConfig(
    int temperature,
    double topK,
    double topP,
    int maxOutputTokens,
    string responseMimeType
    );