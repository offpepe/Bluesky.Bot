using bsky.bot.Clients.Objects;

namespace bsky.bot.Clients.Responses;

public readonly record struct GeminiGeneratedResponse(
    GeminiCandidate[] candidates);

public readonly record struct GeminiCandidate(
    GeminiResponseContent content,
    string finishReason,
    int index
    );
    
public readonly record struct GeminiResponseContent(
    GeminiRequestPart[] parts,
    string role
    );