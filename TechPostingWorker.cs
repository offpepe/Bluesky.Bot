using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Storage;

namespace bsky.bot;

public class TechPostingWorker(BlueSky blueSky, DataRepository dataRepository, string ollamaUrl)
{
    private readonly Ollama _ollama = new (ollamaUrl, OllamaModels.SOCIAL_POSTING_MODEL);
    private readonly Gemini _gemini = new Gemini("gemini-1.5-flash");
    
    private readonly ILogger<TechPostingWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<TechPostingWorker>();

    public async Task ExecuteAsync()
    {
        await CreateTechPostJob();
    }

    private async Task CreateTechPostJob()
    {
        _logger.LogInformation("start creating Tech posting job");
        _logger.LogInformation("generating posting job");
        var geminiResponse = await _gemini.GenerateTechContent();
        if (geminiResponse.candidates[0].finishReason != "STOP")
        {
            throw new Exception("Tech post job generation failed");
        }
        var generatedPost = geminiResponse.candidates[0].content.parts[0].text;
        _logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPost(generatedPost);
        _logger.LogInformation("tech posting created");
    }

}