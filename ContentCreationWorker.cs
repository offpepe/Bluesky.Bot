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
    private readonly ILogger<ContentCreationWorker> _logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<ContentCreationWorker>();

    
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
        if (string.IsNullOrEmpty(generatedContent.response) ||
            generatedContent.response.StartsWith("desculpe", StringComparison.CurrentCultureIgnoreCase))
            throw new ApplicationException("Could not generate content, AI failed");
        if (!generatedContent.done) return;
        var generatedPostContent = generatedContent.response;
        if (generatedContent.response.Length > Constants.GENERATED_CONTENT_SIZE)
            generatedPostContent = await this._ollama.AdjustContentSize(generatedPostContent, generatedContent.context);
        generatedPostContent += '\n';
        _logger.LogInformation("Content generated");
        _logger.LogInformation("Publishing content generated");
        await blueSky.CreateNewPost(generatedPostContent, contentBase.Value.href);
        dataRepository.DefineSourceReaded();
        _logger.LogInformation("Content published");
    }

}