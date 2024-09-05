using bsky.bot.Clients;
using bsky.bot.Storage;

namespace bsky.bot;

public class Program
{
    private static readonly string BlueSky = Environment.GetEnvironmentVariable("bluesky_url") ?? throw new ApplicationException("variable $bluesky_url not found");
    private static readonly string OllamaUrl = Environment.GetEnvironmentVariable("ollama_url") ?? throw new ApplicationException("variable $ollama_url not found");
    private static readonly string Email = Environment.GetEnvironmentVariable("bluesky_email") ?? throw new ApplicationException("variable $bluesky_email not found");
    private static readonly string Password = Environment.GetEnvironmentVariable("bluesky_password") ?? throw new ApplicationException("variable $bluesky_password not found");
    private static readonly string EmbbedSourceExtractorUrl = Environment.GetEnvironmentVariable("embbed_source_url") ?? throw new ApplicationException("variable $embbed_source_url not found");
    private static readonly bool BotAccount =
        bool.TryParse(Environment.GetEnvironmentVariable("bot_account"), out var botAccount) && botAccount;  
    
    private static readonly ILogger<Program> Logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<Program>();
    
    public static void Main(string[] args) => MainAsync(args).Wait();
    private static async Task MainAsync(string[] args)
    {
        Logger.LogInformation(@"Worker ""bsky.bot"" started at: {0}", DateTime.Now);
        using var dataRepository = new DataRepository();
        var blueSkyApi = new BlueSky(BlueSky, Email, Password, EmbbedSourceExtractorUrl);
        await blueSkyApi.Login();
        switch (args.ElementAtOrDefault(0))
        {
            case "scr":
                await new ContentCreationWorker(blueSkyApi, dataRepository).ExecuteAsync();
                break;
            case "tp":
                await new TechPostingWorker(blueSkyApi, dataRepository, OllamaUrl).ExecuteAsync();
                break;
            default:
                await new InteractionWorker(blueSkyApi, dataRepository, BotAccount).ExecuteAsync();
                break;
        }
        Logger.LogInformation(@"Worker ""bsky.bot"" ended at: {0}", DateTime.Now);
    }
    
}