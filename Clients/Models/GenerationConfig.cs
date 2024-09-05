namespace bsky.bot.Clients.Models;

public readonly record struct GenerationConfig(
    double temperature,
    double topK,
    double topP,
    int maxOutputTokens,
    string responseMimeType
    );