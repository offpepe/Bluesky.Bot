using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public class VerifyTechContentRequest : LLMRequest
{
    public VerifyTechContentRequest(string content)
    {
        contents =
        [
            new GeminiInstruction("user", [
                new GeminiRequestPart(content)
            ])
        ];
        systemInstruction = new GeminiInstruction(
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.VerifyTechContent)]
        );
        generationConfig = new GenerationConfig(
            2,
            64,
            0.95,
            8192,
            "text/plain"
        );
    }
}