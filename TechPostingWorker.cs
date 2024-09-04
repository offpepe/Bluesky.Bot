using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Storage;

namespace bsky.bot;

public class TechPostingWorker(BlueSky blueSky, DataRepository dataRepository, string ollamaUrl)
{
    private readonly Ollama _ollama = new (ollamaUrl, OllamaModels.SOCIAL_POSTING_MODEL);
    
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
        var ollamaResponse = await _ollama.GenerateReply("Gere novo post");
        if (!ollamaResponse.done || string.IsNullOrEmpty(ollamaResponse.response) ||
            ollamaResponse.response.StartsWith("desculpe", StringComparison.CurrentCultureIgnoreCase))
            throw new ApplicationException("Could not generate content, AI failed");
        _logger.LogInformation("post generated");
        var generatedResponse = ollamaResponse.response;
        if (generatedResponse.Length > Constants.GENERATED_CONTENT_SIZE)
            generatedResponse = await _ollama.AdjustContentSize(generatedResponse, ollamaResponse.context);
        _logger.LogInformation("posting content");
        generatedResponse = generatedResponse[1..][..^1] + '\n';
        await blueSky.CreateNewSocialPost(generatedResponse);
        _logger.LogInformation("tech posting created");
    }

}