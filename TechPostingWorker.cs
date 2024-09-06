using bsky.bot.Clients;
using bsky.bot.Clients.Enums;
using bsky.bot.Clients.Interface;
using bsky.bot.Clients.Requests.Gemini;
using bsky.bot.Storage;

namespace bsky.bot;

public class TechPostingWorker(BlueSky blueSky, ILllmModel model)
{
    
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
        var generatedPost = await model.Generate(new TechPostRequest());
        _logger.LogInformation("posting content");
        await blueSky.CreateNewSocialPost(generatedPost);
        _logger.LogInformation("tech posting created");
    }

}