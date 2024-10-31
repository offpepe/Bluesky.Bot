using bsky.bot.Clients;
using bsky.bot.Clients.Interface;
using bsky.bot.Storage;
using bsky.bot.Workers;

namespace bsky.bot;

public class Program
{
    private static readonly string blueSky = Environment.GetEnvironmentVariable("bluesky_url") ?? throw new ApplicationException("variable $bluesky_url not found");
    private static readonly string ollamaUrl = Environment.GetEnvironmentVariable("ollama_url") ?? throw new ApplicationException("variable $ollama_url not found");
    private static readonly string email = Environment.GetEnvironmentVariable("bluesky_email") ?? throw new ApplicationException("variable $bluesky_email not found");
    private static readonly string password = Environment.GetEnvironmentVariable("bluesky_password") ?? throw new ApplicationException("variable $bluesky_password not found");
    private static readonly string embbedSourceExtractorUrl = Environment.GetEnvironmentVariable("embbed_source_url") ?? throw new ApplicationException("variable $embbed_source_url not found");
    private static readonly string llmModel = Environment.GetEnvironmentVariable("model") ?? "gemini";

    private static readonly ILogger<Program> logger = LoggerFactory.Create(b =>
    {
        b.SetMinimumLevel(LogLevel.Debug).AddSimpleConsole();
    }).CreateLogger<Program>();

    public static void Main() => MainAsync().Wait();
    private static async Task MainAsync()
    {
        logger.LogInformation("Worker \"bsky.bot\" started at: {0}", DateTime.Now);
        using var dataRepository = new DataRepository();
        var blueSkyApi = new BlueSky(blueSky, email, password, embbedSourceExtractorUrl);
        ILllmModel model = llmModel == "gemini" ? new Gemini() : new Ollama(ollamaUrl, "llama3.1");
        await blueSkyApi.LoginAsync();
        await new InteractionWorker(blueSkyApi, dataRepository, model).ExecuteAsync();
        logger.LogInformation("Worker \"bsky.bot\" ended at: {0}", DateTime.Now);
    }

}
