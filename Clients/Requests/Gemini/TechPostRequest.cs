using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public sealed class TechPostRequest : GeminiRequest
{
    public TechPostRequest()
    {
        contents =
        [
            new GeminiInstruction("user", [
                new GeminiRequestPart("Gere novo post")
            ])
        ];
        systemInstruction = new GeminiInstruction(
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.CreateTechPost)]
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

