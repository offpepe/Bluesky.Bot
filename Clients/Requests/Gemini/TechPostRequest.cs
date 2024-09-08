using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public sealed class TechPostRequest : LLMRequest
{
    public TechPostRequest(string context)
    {
        contents =
        [
            new GeminiInstruction("user", [
                new GeminiRequestPart(context)
            ])
        ];
        systemInstruction = new GeminiInstruction(
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.CreateTechPost)]
        );
        generationConfig = new GenerationConfig(
            2,
            64,
            0.95,
            8192,
            "text/plain"
        );
    }
    
    public TechPostRequest(GeminiInstruction[] instruction)
    {
        contents = instruction;
        systemInstruction = new GeminiInstruction(
            "user",
            [new GeminiRequestPart(GeminiSystemInstructions.CreateTechPost)]
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

