namespace bsky.bot;

public class Program()
{
    private static readonly string BlueSky = Environment.GetEnvironmentVariable("bluesky_url") ?? throw new ApplicationException("variable $bluesky_url not found");
    private static readonly string OllamaUrl = Environment.GetEnvironmentVariable("ollama_url") ?? throw new ApplicationException("variable $ollama_url not found");
    private static readonly string Email = Environment.GetEnvironmentVariable("bluesky_email") ?? throw new ApplicationException("variable $bluesky_email not found");
    private static readonly string Password = Environment.GetEnvironmentVariable("bluesky_password") ?? throw new ApplicationException("variable $bluesky_password not found");

    
    public static void Main()
        => MainAsync().Wait();
    private static async Task MainAsync()
    {
        using var interactionWorker = new InteractionWorker(BlueSky, OllamaUrl, Email, Password);
        var contentCreationWorker = new ContentCreationWorker(BlueSky, OllamaUrl, Email, Password);
        await interactionWorker.ExecuteAsync();
        await contentCreationWorker.ExecuteAsync();
    }
}