using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public readonly struct GeminiGeneratePostReplyRequest
{
    public GeminiGeneratePostReplyRequest(GeminiInstruction[] context)
    {
        contents = context;
    }
    public GeminiInstruction systemInstruction { get; } = new (
        "user",
        [new GeminiRequestPart(GeminiSystemInstructions.ReplyPost)]
    );

    public GeminiInstruction[] contents { get; init; }

    public GenerationConfig generationConfig { get; } = new (
        2,
        64,
        0.95,
        8192,
        "text/plain"
    );
}