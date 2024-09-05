using System.Runtime.Intrinsics.X86;
using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Models;
using bsky.bot.Storage;
using bsky.bot.Storage.Models;

namespace bsky.bot;

public class ContentCreationWorker(
    BlueSky blueSky,
    DataRepository dataRepository
)
{
    private readonly Gemini _gemini = new Gemini("gemini-1.5-flash");
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
        var generatedContent = await _gemini.GenerateArticleSummary(contentBase.Value.source);
        _logger.LogInformation("Content generated");
        if (generatedContent.Length > Constants.GENERATED_CONTENT_SIZE)
            generatedContent = await AdjustContentSize(contentBase.Value.source, generatedContent);
        _logger.LogInformation("Publishing content generated");
        await blueSky.CreateNewContentPost(generatedContent + '\n', contentBase.Value.href);
        dataRepository.DefineSourceReaded();
        _logger.LogInformation("Content published");
    }


    private async Task<string> AdjustContentSize(string contentBase, string generatedContent)
    {
        var instructions = new List<GeminiInstruction>()
        {
            new ("user", [new GeminiRequestPart(contentBase)]),
            new ("model", [new GeminiRequestPart(generatedContent)]),
        };
        var attempts = 1;        
        while (true)
        {
            instructions.Add(new GeminiInstruction("user", [new GeminiRequestPart("Resuma mais")]));
            _logger.LogInformation("Adjusting content size, attempt: {Attempt}", attempts);
            generatedContent = await _gemini.Summarize(
                GeminiSystemInstructions.CreateArticleSummary,
                instructions
            );
            if (generatedContent.Length <= Constants.GENERATED_CONTENT_SIZE) break;
            instructions.Add(new GeminiInstruction("model", [new GeminiRequestPart(generatedContent)]));
            attempts++;
        }
        return generatedContent;
    }
}