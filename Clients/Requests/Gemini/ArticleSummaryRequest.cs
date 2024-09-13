using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public sealed class ArticleSummaryRequest : LLMRequest
{
    public ArticleSummaryRequest(string articleContent)
    {
        contents = [
            new GeminiInstruction("user", [new GeminiRequestPart(articleContent)])
        ];
        systemInstruction = new (
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.CreateArticleSummary)]
        );
        generationConfig = new GenerationConfig(
            1.5,
            64,
            0.95,
            8192,
            "text/plain"
        );
    }
}