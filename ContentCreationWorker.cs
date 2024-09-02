using System.Runtime.Intrinsics.X86;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Storage;

namespace bsky.bot;

public class ContentCreationWorker(string url, string ollamaUrl, string email, string password)
{
    private readonly BlueSky _blueSky = new (url, email, password);
    private readonly Ollama _ollama = new (ollamaUrl, OllamaModels.CONTENT_CREATION_MODEL);
    private readonly DataRepository _dataRepository = new ();
    
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    
    public async Task ExecuteAsync()
    {
        await CreateContentBasedOnSource();
    }

    private async Task CreateContentBasedOnSource()
    {
        _logger.LogInformation("Starting content creation");
        var contentBase = _dataRepository.GetContentSource();
        if (contentBase == null)
        {
            _logger.LogError("Unreaded content source not found, cannot create content");
            return;
        }
        _logger.LogInformation("Generating content based on source");
        var generatedContent = await _ollama.GenerateReply(contentBase);
        if (!generatedContent.done) return;
        var generatedPostContent = generatedContent.response;
        if (generatedContent.response.Length > 250)
            generatedPostContent = await AdjustContentSize(generatedPostContent);
        _logger.LogInformation("Content generated");
        _logger.LogInformation("Publishing content generated");
        await _blueSky.CreateNewPost(generatedPostContent);
        _logger.LogInformation("Content published");
    }

    private async Task<string> AdjustContentSize(string generatedResult)
    {
        _logger.LogInformation("Adjusting content size");
        var adjustedContent = await _ollama.GenerateReply($"Resuma: {generatedResult}");
        if (adjustedContent is { done: true, response.Length: > 250 }) return await AdjustContentSize(adjustedContent.response);
        return adjustedContent.response;
    }
}