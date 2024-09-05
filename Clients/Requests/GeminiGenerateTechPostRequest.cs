using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests;

public readonly struct GeminiGenerateTechPostRequest
{
    public GeminiGenerateTechPostRequest() { /* ignore */ }
    public GeminiInstruction systemInstruction { get; } = new GeminiInstruction(
        "user",
        [new GeminiRequestPart(GeminiSystemInstructions.CreateTechPost)]
        );

    public GeminiInstruction[] contents { get; } =
    [
        new ("user", [
            new GeminiRequestPart("Gere novo post")
        ])
    ];

    public GenerationConfig generationConfig { get; } = new (
        1.5,
        64,
        0.95,
        8192,
        "text/plain"
    );

}

