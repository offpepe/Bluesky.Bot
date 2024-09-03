using System.Runtime.Intrinsics.X86;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Storage;
using bsky.bot.Storage.Models;

namespace bsky.bot;

public class ContentCreationWorker(
    BlueSky blueSky,
    DataRepository dataRepository,
    string ollamaUrl
)
{
    private readonly Ollama _ollama = new (ollamaUrl, OllamaModels.CONTENT_CREATION_MODEL);
    private readonly ILogger<InteractionWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<InteractionWorker>();

    private const int GENERATED_CONTENT_SIZE = 250;
    
    public async Task ExecuteAsync()
    {
        await CreateContentBasedOnSource();
    }

    private async Task CreateContentBasedOnSource()
    {
        _logger.LogInformation("Starting content creation");
        var contentBase = dataRepository.GetContentSource();
        if (contentBase == null)
        {
            _logger.LogInformation("Unreaded content source not found, cannot create content");
            return;
        }
        _logger.LogInformation("Generating content based on source");
        var generatedContent = await _ollama.GenerateReply(contentBase.Value.source);
        if (generatedContent.response.ToLower().StartsWith("desculpe"))
        {
            await CreateContentBasedOnSource();
            return;
        }
        if (!generatedContent.done) return;
        var generatedPostContent = generatedContent.response + '\n';
        if (generatedContent.response.Length > GENERATED_CONTENT_SIZE)
            generatedPostContent = await AdjustContentSize(generatedPostContent, generatedContent.context); 
        _logger.LogInformation("Content generated");
        _logger.LogInformation("Publishing content generated");
        await blueSky.CreateNewPost(generatedPostContent, contentBase.Value.href);
        dataRepository.DefineSourceReaded();
        _logger.LogInformation("Content published");
    }

    private async Task<string> AdjustContentSize(string generatedResult, int[] context)
    {
        _logger.LogInformation("Adjusting content size");
        var adjustedContent = await _ollama.GenerateReply($"Resuma mais", context);
        if (adjustedContent is { done: true, response.Length: > GENERATED_CONTENT_SIZE }) return await AdjustContentSize(adjustedContent.response, context);
        return adjustedContent.response;
    }
}