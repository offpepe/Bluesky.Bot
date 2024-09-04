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
        var interactionWorker = new InteractionWorker(blueSkyApi,  dataRepository, OllamaUrl);
        var contentCreationWorker = new ContentCreationWorker(blueSkyApi, dataRepository, OllamaUrl);
        if (args.Any(a => a == "scr"))
        {
            await contentCreationWorker.ExecuteAsync();
        }
        await interactionWorker.ExecuteAsync();
        Logger.LogInformation(@"Worker ""bsky.bot"" ended at: {0}", DateTime.Now);
    }
}