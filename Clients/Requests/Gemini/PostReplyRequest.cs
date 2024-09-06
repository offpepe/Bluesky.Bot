using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public sealed class PostReplyRequest : GeminiRequest
{
    public PostReplyRequest(GeminiInstruction[] context)
    {
        contents = context;
        systemInstruction = new GeminiInstruction(
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.ReplyPost)]
        );
        generationConfig = new GenerationConfig(
            1.3,
            64,
            0.95,
            8192,
            "text/plain"
        );
    }
}