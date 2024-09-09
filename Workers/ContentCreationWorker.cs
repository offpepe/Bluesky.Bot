using bsky.bot.Clients;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Storage;

namespace bsky.bot.Workers;

public class ContentCreationWorker(
    BlueSky blueSky,
    DataRepository dataRepository,
    ILllmModel model)
{
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
        var generatedContent = await model.Generate(new ArticleSummaryRequest(contentBase.Value.source));
        _logger.LogInformation("Publishing content generated");
        await blueSky.CreateNewContentPost(generatedContent + '\n', contentBase.Value.href);
        dataRepository.DefineSourceReaded();
        _logger.LogInformation("Content published");
    }
}