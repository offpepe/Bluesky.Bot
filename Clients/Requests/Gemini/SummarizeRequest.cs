using bsky.bot.Clients.Models;

namespace bsky.bot.Clients.Requests.Gemini;

public sealed class SummarizeRequest : GeminiRequest
{
    public SummarizeRequest(string systemInstruction, GeminiInstruction[] contents)
    {
        this.systemInstruction =  new GeminiInstruction(
            "user",
            [new GeminiRequestPart(systemInstruction)]
        );
        this.contents = contents;
        this.generationConfig = new GenerationConfig(
            1,
            64,
            0.95,
            8192,
            "text/plain"
        );
    }
}