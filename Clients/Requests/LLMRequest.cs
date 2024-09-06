using System.Diagnostics.CodeAnalysis;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public abstract class LLMRequest
{
    public GeminiInstruction systemInstruction { get; init; }
    public GeminiInstruction[] contents { get; init; }
    public GenerationConfig generationConfig { get; init; }
}