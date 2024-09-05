using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public readonly struct SummarizeRequest(string systemInstruction, GeminiInstruction[] contents)
{
    public GeminiInstruction systemInstruction { get; } = new (
        "user",
        [new GeminiRequestPart(systemInstruction)]
    );

    public GeminiInstruction[] contents { get; } = contents;
    public GenerationConfig generationConfig { get; } = new (
        1,
        64,
        0.95,
        8192,
        "text/plain"
    );
}