using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public class GenerateArticleSummary
{
    public GenerateArticleSummary(string articleContent)
    {
        contents = [
            new GeminiInstruction("user", [new GeminiRequestPart(articleContent)])
        ];
    }
    public GeminiInstruction systemInstruction { get; } = new (
        "user",
        [new GeminiRequestPart(GeminiSystemInstructions.CreateArticleSummary)]
    );

    public GeminiInstruction[] contents { get; init; }
    public GenerationConfig generationConfig { get; } = new (
        1,
        64,
        0.95,
        8192,
        "text/plain"
    );
}