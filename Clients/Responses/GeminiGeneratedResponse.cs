using bsky.bot.Clients.Models;

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