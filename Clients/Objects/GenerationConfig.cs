namespace bsky.bot.Clients.Objects;

public readonly record struct GenerationConfig(
    double temperature,
    double topK,
    double topP,
    int maxOutputTokens,
    string responseMimeType
    );